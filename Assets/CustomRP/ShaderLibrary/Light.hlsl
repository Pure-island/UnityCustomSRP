//灯光数据相关库
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64
//多个平行光的属性
CBUFFER_START(_CustomLight)
    //float3 _DirectionalLightColor;
    //float3 _DirectionalLightDirection;
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    //阴影数据
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
    //非定向光源的属性
    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

//灯光的属性
struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

//获取方向光的阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{        
    DirectionalShadowData data;
    //data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    //获取灯光的法线偏差值
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}
//获取其他类型光源的阴影数据
OtherShadowData GetOtherShadowData(int lightIndex)
{
    OtherShadowData data;
    data.strength = _OtherLightShadowData[lightIndex].x;
    data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
    return data;
}

//获取方向光的数量
int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

//获取指定索引的方向光的数据
Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData)
{
    shadowData = UpdateShadowDataCascade(surfaceWS, _DirectionalLightShadowData[index].y, shadowData);
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    //得到阴影数据
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
    //得到阴影衰减
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
    return light;
}

//获取指定索引的非定向光源数据
Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData) 
{
    Light light;
    light.color = _OtherLightColors[index].rgb;
    float3 ray = _OtherLightPositions[index].xyz - surfaceWS.position;
    light.direction = normalize(ray);
    //光照强度随距离衰减
    float distanceSqr = max(dot(ray, ray), 0.00001);
    //套用公式计算随光照范围衰减
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w)));
    float4 spotAngles = _OtherLightSpotAngles[index];
    //计算聚光灯衰减值
    float spotAttenuation = Square(saturate(dot(_OtherLightDirections[index].xyz, light.direction) * spotAngles.x + spotAngles.y));
    OtherShadowData otherShadowData = GetOtherShadowData(index);
    //光照强度随范围和距离衰减
    light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS) * spotAttenuation * rangeAttenuation / distanceSqr;
    return light;
}

//获取非定向光源的数量
int GetOtherLightCount()
{
    return _OtherLightCount;
}
 
#endif