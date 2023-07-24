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
//ʹ��UnityInput������ֶ�ǰ��include����
#include "UnityInput.hlsl"
#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
   #define SHADOWS_SHADOWMASK
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

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
#endif