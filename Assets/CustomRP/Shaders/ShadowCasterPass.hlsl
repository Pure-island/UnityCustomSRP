//��ӰͶ����
#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED

//���㺯������ṹ��
struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
//ƬԪ��������ṹ��
struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

bool _ShadowPancaking;
//���㺯��
Varyings ShadowCasterPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
	//ʹUnlitPassVertex���λ�ú�����,����������
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    if (_ShadowPancaking)
    {
#if UNITY_REVERSED_Z
		    output.positionCS_SS.z = min(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
#else
        output.positionCS_SS.z = max(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
#endif
    }
	
	//�������ź�ƫ�ƺ��UV����
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}
//ƬԪ����
void ShadowCasterPassFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    //ClipLOD(input.positionCS.xy, unity_LODFade.x);
    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
    ClipLOD(config.fragment, unity_LODFade.x);
    float4 base = GetBase(config);
#if defined(_SHADOWS_CLIP)
	//͸���ȵ�����ֵ��ƬԪ��������
	clip(base.a - GetCutoff(config));
#elif defined(_SHADOWS_DITHER)
	//���㶶��ֵ
	float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	clip(base.a - dither);
#endif
	
}

#endif
