//��Ӱ����
#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED
 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
//���ʹ�õ���PCF 3X3
#if defined(_DIRECTIONAL_PCF3)
//��Ҫ4���˲�����
#define DIRECTIONAL_FILTER_SAMPLES 4
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
#define DIRECTIONAL_FILTER_SAMPLES 9
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
#define DIRECTIONAL_FILTER_SAMPLES 16
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#if defined(_OTHER_PCF3)
        #define OTHER_FILTER_SAMPLES 4
        #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_OTHER_PCF5)
        #define OTHER_FILTER_SAMPLES 9
        #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_OTHER_PCF7)
        #define OTHER_FILTER_SAMPLES 16
        #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif
 
#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4

#define MAX_SHADOWED_OTHER_LIGHT_COUNT 16

//��Ӱ��������Ϣ
struct DirectionalShadowData
{
    float strength;
    int tileIndex;
    //����ƫ��
    float normalBias;
    int shadowMaskChannel;
};

struct OtherShadowData
{
    float strength;
    int tileIndex;
    int shadowMaskChannel;
    float3 lightPositionWS;
    float3 spotDirectionWS;
    bool isPoint;
    float3 lightDirectionWS;
};

//�決��Ӱ����
struct ShadowMask
{
    bool always;
    bool distance;
    float4 shadows;
};
 
//�������Ӱ����
struct ShadowData
{
    //�����ȼ�
    int cascadeIndex;
    //����Ӱͼ���е�λ��
    int multi_cascadeIndex;
    //�Ƿ������Ӱ�ı�ʶ
    float strength;
    //��ϼ���
    float cascadeBlend;
    //�決��Ӱ����
    ShadowMask shadowMask;
};

//��Ӱͼ��
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_OtherShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);
 
#define MAX_CASCADE_COUNT 4
CBUFFER_START(_CustomShadows)
//���������Ͱ�Χ������
int _CascadeCount;
float4 _CascadeCullingSpheres[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
//��������
float4 _CascadeData[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
//��Ӱ����
float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
float4x4 _OtherShadowMatrices[MAX_SHADOWED_OTHER_LIGHT_COUNT];
float4 _OtherShadowTiles[MAX_SHADOWED_OTHER_LIGHT_COUNT];
//float _ShadowDistance;
//��Ӱ���ɾ���
float4 _ShadowDistanceFade;
float4 _ShadowAtlasSize;
CBUFFER_END

//��ʽ������Ӱ����ʱ��ǿ��
float FadedShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}
 
//�õ�����ռ�ı�����Ӱ����
ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.shadowMask.always = false;
    data.shadowMask.distance = false;
    data.shadowMask.shadows = 1.0;
    data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    data.cascadeIndex = 0;
    data.multi_cascadeIndex = 0;
    data.cascadeBlend = 1;
    return data;
}

//��ÿ��ƽ�й�Դ�����±�����Ӱ���ݵ�strength�ͼ�����Ӱ����
ShadowData UpdateShadowDataCascade(Surface surfaceWS, int indexBegin, ShadowData data)
{
    int i;
    //���������浽���ĵ�ƽ������С������뾶��ƽ������˵������������㼶����Χ���У��õ����ʵļ����㼶����
    for (i = 0; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[indexBegin + i];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (distanceSqr < sphere.w)
        {
            //���㼶����Ӱ�Ĺ���ǿ��
            float fade = FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
            //������ƵĶ��������һ�������ķ�Χ�У����㼶���Ĺ�����Ӱǿ�ȣ�����Ӱ������Ĺ�����Ӱǿ����˵õ�������Ӱǿ��
            if (i == _CascadeCount - 1)
            {
                data.strength *= fade;
            }
            else
            {
                data.cascadeBlend = fade;
            }
            break;
        }
    }
    //��������������Χ�Ҽ�����������0����ȫ����Ӱǿ����Ϊ0(��������Ӱ����)  
    if (i == _CascadeCount && _CascadeCount > 0)
    {
        data.strength = 0.0;
    }
#if defined(_CASCADE_BLEND_DITHER)
    else if (data.cascadeBlend < surfaceWS.dither) 
    {
        i += 1;
    }
#endif
#if !defined(_CASCADE_BLEND_SOFT)
    data.cascadeBlend = 1.0;
#endif
    data.cascadeIndex = i;
    data.multi_cascadeIndex = indexBegin + i;
    return data;
}


//������Ӱͼ��
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS)
{
    #if defined(DIRECTIONAL_FILTER_SETUP)
    //����Ȩ��
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    //����λ��
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) 
    {
        //�������������õ�Ȩ�غ�
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
    }
    return shadow;
#else
    return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

float SampleOtherShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(_OtherShadowAtlas, SHADOW_SAMPLER, positionSTS);
}
 
float FilterOtherShadow(float3 positionSTS, float3 bounds)
{
#if defined(OTHER_FILTER_SETUP)
    //����Ȩ��
    real weights[OTHER_FILTER_SAMPLES];
        //����λ��
        real2 positions[OTHER_FILTER_SAMPLES];
        float4 size = _ShadowAtlasSize.wwzz;
        OTHER_FILTER_SETUP(size, positionSTS.xy, weights, positions);
        float shadow = 0;
        for (int i = 0; i < OTHER_FILTER_SAMPLES; i++) 
        {
            //�������������õ�Ȩ�غ�
            shadow += weights[i] * SampleOtherShadowAtlas(float3(positions[i].xy, positionSTS.z), bounds);
        }
        return shadow;
#else
    return SampleOtherShadowAtlas(positionSTS, bounds);
#endif
}

//�õ��決��Ӱ��˥��ֵ
float GetBakedShadow(ShadowMask mask, int channel)
{
    float shadow = 1.0;
    if (mask.always || mask.distance)
    {
        if (channel >= 0)
        {
            shadow = mask.shadows[channel];
        }
    }
    return shadow;
}
float GetBakedShadow(ShadowMask mask, int channel, float strength)
{
    if (mask.always || mask.distance)
    {
        return lerp(1.0, GetBakedShadow(mask, channel), strength);
    }
    return 1.0;
}

float GetCascadedShadow(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
    //���㷨��ƫ��
    float3 normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[global.multi_cascadeIndex].y);
    //���Ϸ���ƫ�ƺ�ı��涥��λ�ã�ͨ����Ӱת������ͱ���λ�õõ�����Ӱ����(ͼ��)�ռ��λ�� �õ�����Ӱ����ռ����λ�ã�Ȼ���ͼ�����в���  
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surfaceWS.position + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    if (global.cascadeBlend < 1.0)
    {
        normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[global.multi_cascadeIndex + 1].y);
        positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
    }
    return shadow;
}

//��Ϻ決��ʵʱ��Ӱ
float MixBakedAndRealtimeShadows(ShadowData global, float shadow, int shadowMaskChannel, float strength)
{
    float baked = GetBakedShadow(global.shadowMask, shadowMaskChannel);
    if (global.shadowMask.always)
    {
        shadow = lerp(1.0, shadow, global.strength);
        shadow = min(baked, shadow);
        return lerp(1.0, shadow, strength);
    }
    if (global.shadowMask.distance)
    {
        shadow = lerp(baked, shadow, global.strength);
        return lerp(1.0, shadow, strength);
    }
    return lerp(1.0, shadow, strength * global.strength);
}

//������Ӱ˥��
float GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
    //�����������Ӱ����Ӱ˥��Ϊ1
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif
    float shadow;
    if (directional.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, directional.shadowMaskChannel, abs(directional.strength));
    }
    else
    {
        shadow = GetCascadedShadow(directional, global, surfaceWS);
        //shadow = lerp(1.0, shadow, directional.strength);
        //��Ӱ���
        shadow = MixBakedAndRealtimeShadows(global, shadow, directional.shadowMaskChannel, directional.strength);
    }
    return shadow;
}

static const float3 pointShadowPlanes[6] =
{
    float3(-1.0, 0.0, 0.0),
    float3(1.0, 0.0, 0.0),
    float3(0.0, -1.0, 0.0),
    float3(0.0, 1.0, 0.0),
    float3(0.0, 0.0, -1.0),
    float3(0.0, 0.0, 1.0)
};
//�õ��Ƕ����Դ��ʵʱ��Ӱ˥��
float GetOtherShadow(OtherShadowData other, ShadowData global, Surface surfaceWS)
{
    float tileIndex = other.tileIndex;
    float3 lightPlane = other.spotDirectionWS;
    if (other.isPoint)
    {
        float faceOffset = CubeMapFaceID(-other.lightDirectionWS);
        tileIndex += faceOffset;
        lightPlane = pointShadowPlanes[faceOffset];
    }
    float4 tileData = _OtherShadowTiles[tileIndex];
    float3 surfaceToLight = other.lightPositionWS - surfaceWS.position;
    float distanceToLightPlane = dot(surfaceToLight, lightPlane);
    //float3 normalBias = surfaceWS.normal * (distanceToLightPlane * tileData.w);
    float3 normalBias = surfaceWS.normal * (distanceToLightPlane * 1.5* tileData.w);
    float4 positionSTS = mul(_OtherShadowMatrices[tileIndex], float4(surfaceWS.position + normalBias, 1.0));
    //͸��ͶӰ���任λ�õ�XYZ����Z
    return FilterOtherShadow(positionSTS.xyz / positionSTS.w, tileData.xyz);
}

//�õ��������͹�Դ����Ӱ˥��
float GetOtherShadowAttenuation(OtherShadowData other, ShadowData global, Surface surfaceWS)
{
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif
 
    float shadow;
    if (other.strength > 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, other.shadowMaskChannel, other.strength);
    }
    else
    {
        shadow = 1.0;
    }
    if (other.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, other.shadowMaskChannel, abs(other.strength));
    }
    else
    {
        shadow = GetOtherShadow(other, global, surfaceWS);
        shadow = MixBakedAndRealtimeShadows(global, shadow, other.shadowMaskChannel, other.strength);
    }
    return shadow;
}

#endif