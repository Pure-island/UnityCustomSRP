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
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
//使用UnityInput里面的字段前先include进来
#include "UnityInput.hlsl"
#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
   #define SHADOWS_SHADOWMASK
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);
SAMPLER(sampler_CameraColorTexture);
//根据unity_OrthoParams的W分量是0还是1判断是否使用的是正交相机
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

//解码法线数据，得到原来的法线向量
float3 DecodeNormal(float4 sample, float scale)
{
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(sample, scale);
#else
    return UnpackNormalmapRGorAG(sample, scale);
#endif
}

//将法线从切线空间转换到世界空间
float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
    //构建切线到世界空间的转换矩阵，需要世界空间的法线、世界空间的切线的XYZ和W分量
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