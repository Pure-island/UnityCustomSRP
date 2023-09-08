//计算光照相关库
#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

//检测表面掩码和灯光掩码是否重叠
bool RenderingLayersOverlap(Surface surface, Light light)
{
    return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

//得到最终照明结果
float3 GetLighting(Surface surfaceWS, BRDF brdf, GI gi)
{
    //得到表面阴影数据
    ShadowData shadowData = GetShadowData(surfaceWS);
    shadowData.shadowMask = gi.shadowMask;
    //return gi.shadowMask.shadows.rgb;
    //可见光的光照结果进行累加得到最终光照结果
    float3 color = IndirectBRDF(surfaceWS, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        if (RenderingLayersOverlap(surfaceWS, light))
        {
            color += GetLighting(surfaceWS, brdf, light);
        }
    }
 
#if defined(_LIGHTS_PER_OBJECT)
    for (int j = 0; j < min(unity_LightData.y, 8); j++) 
    {
        int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4];
        Light light = GetOtherLight(lightIndex, surfaceWS, shadowData);
        if (RenderingLayersOverlap(surfaceWS, light))  
        {
            color += GetLighting(surfaceWS, brdf, light); 
        }
    }
 
#else
    for (int j = 0; j < GetOtherLightCount(); j++)
    {
        Light light = GetOtherLight(j, surfaceWS, shadowData);
        if (RenderingLayersOverlap(surfaceWS, light))
        {
            color += GetLighting(surfaceWS, brdf, light);
        }
    }
 
#endif
    return color;
}
#endif