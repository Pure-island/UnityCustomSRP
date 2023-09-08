#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

//���㺯������ṹ��
struct Attributes
{
    float3 positionOS : POSITION;
    float4 color : COLOR;
    float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
//ƬԪ��������ṹ��
struct Varyings
{
    float4 positionCS : SV_POSITION;
#if defined(_VERTEX_COLORS)
	float4 color : VAR_COLOR;
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
    output.positionCS = TransformWorldToHClip(positionWS);
#if defined(_VERTEX_COLORS)
	output.color = input.color;
#endif
	//�������ź�ƫ�ƺ��UV����
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}
//ƬԪ����
float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.baseUV);
    float4 base = GetBase(config);
#if defined(_CLIPPING)
	//͸���ȵ�����ֵ��ƬԪ��������
	clip(base.a - GetCutoff(config));
#endif
    return float4(base.rgb, GetFinalAlpha(base.a));
}

#endif
