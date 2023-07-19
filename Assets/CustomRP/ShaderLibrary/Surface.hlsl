#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED
 
struct Surface
{
    float3 normal;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
    float3 viewDirection;
    //表面位置
    float3 position;
    //表面深度
    float depth;
    //抖动属性
    float dither;
};
 
#endif