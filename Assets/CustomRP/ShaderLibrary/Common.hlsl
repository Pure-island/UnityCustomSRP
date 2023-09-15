//Unity����������
#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED

//����һЩ��ȡ�����õ�ת������
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
//ʹ��UnityInput������ֶ�ǰ��include����
#include "UnityInput.hlsl"
#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
   #define SHADOWS_SHADOWMASK
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);
SAMPLER(sampler_CameraColorTexture);
//����unity_OrthoParams��W������0����1�ж��Ƿ�ʹ�õ����������
bool IsOrthographicCamera()
{
    return unity_OrthoParams.w;
}
float OrthographicDepthBufferToLinear(float rawDepth)
{
#if UNITY_REVERSED_Z
        rawDepth = 1.0 - rawDepth;
#endif
    return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}
#include "Fragment.hlsl"

////�������ܣ������ģ�Ϳռ�ת��������ռ�
//float3 TransformObjectToWorld(float3 positionOS)
//{
//	return mul(unity_ObjectToWorld, float4(positionOS,1.0)).xyz;
//}

////�������ܣ����������ռ�ת�����ü��ռ�
//float4 TransformWorldToClip(float3 positionWS)
//{
//	return mul(unity_MatrixVP, float4(positionWS,1.0));
//}
float Square(float v)
{
    return v * v;
}

//�������������ƽ��
float DistanceSquared(float3 pA, float3 pB)
{
    return dot(pA - pB, pA - pB);
}

//���뷨�����ݣ��õ�ԭ���ķ�������
float3 DecodeNormal(float4 sample, float scale)
{
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(sample, scale);
#else
    return UnpackNormalmapRGorAG(sample, scale);
#endif
}

//�����ߴ����߿ռ�ת��������ռ�
float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
    //�������ߵ�����ռ��ת��������Ҫ����ռ�ķ��ߡ�����ռ�����ߵ�XYZ��W����
    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    return TransformTangentToWorld(normalTS, tangentToWorld);
}

void ClipLOD(Fragment fragment, float fade)
{
#if defined(LOD_FADE_CROSSFADE)
        float dither = InterleavedGradientNoise(fragment.positionSS, 0);
        clip(fade + (fade < 0.0 ? dither : -dither));
#endif
}

#endif