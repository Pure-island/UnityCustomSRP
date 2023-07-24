//Unity公共方法库
#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED

//定义一些宏取代常用的转换矩阵
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
//使用UnityInput里面的字段前先include进来
#include "UnityInput.hlsl"
#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
   #define SHADOWS_SHADOWMASK
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

////函数功能：顶点从模型空间转换到世界空间
//float3 TransformObjectToWorld(float3 positionOS)
//{
//	return mul(unity_ObjectToWorld, float4(positionOS,1.0)).xyz;
//}

////函数功能：顶点从世界空间转换到裁剪空间
//float4 TransformWorldToClip(float3 positionWS)
//{
//	return mul(unity_MatrixVP, float4(positionWS,1.0));
//}
float Square(float v)
{
    return v * v;
}

//计算两点间距离的平方
float DistanceSquared(float3 pA, float3 pB)
{
    return dot(pA - pB, pA - pB);
}
#endif