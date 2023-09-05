//unity标准输入库
#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED
CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;
//这个矩阵包含一些在这里我们不需要的转换信息
real4 unity_WorldTransformParams;
float4 unity_ProbesOcclusion;
float4 unity_LightmapST;
float4 unity_DynamicLightmapST;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_ProbeVolumeParams;
float4x4 unity_ProbeVolumeWorldToObject;
float4 unity_ProbeVolumeSizeInv;
float4 unity_ProbeVolumeMin;
float4 unity_SpecCube0_HDR;
//unity_LightData的Y分量中包含了灯光数量， unity_LightIndices的两个分量都包含一个光源索引，所以每个对象最多支持8个。
real4 unity_LightData;
real4 unity_LightIndices[2];
float4 _ProjectionParams;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
//相机位置
float3 _WorldSpaceCameraPos;
#endif
