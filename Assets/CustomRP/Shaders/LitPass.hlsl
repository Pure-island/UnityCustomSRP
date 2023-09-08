#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"


//���㺯������ṹ��
struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
	//���淨��
    float3 normalOS : NORMAL;
	//����ռ������
    float4 tangentOS : TANGENT;
	//������ͼ��UV����
	GI_ATTRIBUTE_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
//ƬԪ��������ṹ��
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float2 baseUV : VAR_BASE_UV;
#if defined(_DETAIL_MAP)
	float2 detailUV : VAR_DETAIL_UV;
#endif
	//���編��
    float3 normalWS : VAR_NORMAL;
#if defined(_NORMAL_MAP)
	//����ռ������
	float4 tangentWS : VAR_TANGENT;
#endif
	GI_VARYINGS_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


//���㺯��
Varyings LitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
	TRANSFER_GI_DATA(input, output);
	//ʹUnlitPassVertex���λ�ú�����,����������
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
	//��������ռ�ķ���
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_NORMAL_MAP)
	//��������ռ������
	output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif
	//�������ź�ƫ�ƺ��UV����
    output.baseUV = TransformBaseUV(input.baseUV);
#if defined(_DETAIL_MAP)
	output.detailUV = TransformDetailUV(input.baseUV);
#endif
    return output;
}
//ƬԪ����
float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    ClipLOD(input.positionCS.xy, unity_LODFade.x);
    InputConfig config = GetInputConfig(input.baseUV);
#if defined(_DETAIL_MAP)
	config.detailUV = input.detailUV;
	config.useDetail = true;
#endif
#if defined(_MASK_MAP)
	config.useMask = true;
#endif
    float4 base = GetBase(config);
#if defined(_CLIPPING)
	//͸���ȵ�����ֵ��ƬԪ��������
	clip(base.a - GetCutoff(config));
#endif
	//����һ��surface���������
    Surface surface;
    surface.position = input.positionWS;
#if defined(_NORMAL_MAP)
	surface.normal = NormalTangentToWorld(GetNormalTS(config), input.normalWS, input.tangentWS);
	surface.interpolatedNormal = input.normalWS;
#else
    surface.normal = normalize(input.normalWS);
    surface.interpolatedNormal = surface.normal;
#endif
	//�õ��ӽǷ���
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	//��ȡ�������
    surface.depth = -TransformWorldToView(input.positionWS).z;
    surface.color = base.rgb;
    surface.alpha = base.a;
    surface.metallic = GetMetallic(config);
    surface.occlusion = GetOcclusion(config);
    surface.smoothness = GetSmoothness(config);
    surface.fresnelStrength = GetFresnel(config);
	//���㶶��ֵ
    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);
    surface.renderingLayerMask = asuint(unity_RenderingLayer.x);
	//ͨ���������Ժ�BRDF�������չ��ս��
#if defined(_PREMULTIPLY_ALPHA)
	BRDF brdf = GetBRDF(surface, true);
#else
    BRDF brdf = GetBRDF(surface);
#endif
	//��ȡȫ������
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    float3 color = GetLighting(surface, brdf, gi);
    color += GetEmission(config);
    return float4(color, GetFinalAlpha(surface.alpha));
}

#endif
