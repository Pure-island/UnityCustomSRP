#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

//���㺯������ṹ��
struct Attributes
{
    float3 positionOS : POSITION;
    float4 color : COLOR;
#if defined(_FLIPBOOK_BLENDING)
    float4 baseUV : TEXCOORD0;
    float flipbookBlend : TEXCOORD1;
#else
    float2 baseUV : TEXCOORD0;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
//ƬԪ��������ṹ��
struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
#if defined(_VERTEX_COLORS)
	float4 color : VAR_COLOR;
#endif
#if defined(_FLIPBOOK_BLENDING)
    float3 flipbookUVB : VAR_FLIPBOOK;
#endif
    float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


//���㺯��
Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
	//ʹUnlitPassVertex���λ�ú�����,����������
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
#if defined(_VERTEX_COLORS)
	output.color = input.color;
#endif
	//�������ź�ƫ�ƺ��UV����
    output.baseUV = TransformBaseUV(input.baseUV);
#if defined(_FLIPBOOK_BLENDING)
    output.flipbookUVB.xy = TransformBaseUV(input.baseUV.zw);
    output.flipbookUVB.z = input.flipbookBlend;
#endif
    return output;
}
//ƬԪ����
float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
    //return float4(config.fragment.bufferDepth.xxx / 20.0, 1.0);
#if defined(_VERTEX_COLORS)
    config.color = input.color;
#endif
#if defined(_FLIPBOOK_BLENDING)
    config.flipbookUVB = input.flipbookUVB;
    config.flipbookBlending = true;
#endif
#if defined(_NEAR_FADE)
    config.nearFade = true;
#endif
    #if defined(_SOFT_PARTICLES)
    config.softParticles = true;
#endif
    float4 base = GetBase(config);
#if defined(_CLIPPING)
	//͸���ȵ�����ֵ��ƬԪ��������
	clip(base.a - GetCutoff(config));
#endif
#if defined(_DISTORTION)
    float2 distortion = GetDistortion(config) * base.a;
    base.rgb = lerp(GetBufferColor(config.fragment, distortion).rgb, base.rgb,saturate(base.a - GetDistortionBlend(config)));
#endif
    return float4(base.rgb, GetFinalAlpha(base.a));
}

#endif
