//�ƹ�������ؿ�
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64
//���ƽ�й������
CBUFFER_START(_CustomLight)
    //float3 _DirectionalLightColor;
    //float3 _DirectionalLightDirection;
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    //��Ӱ����
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
    //�Ƕ����Դ������
    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

//�ƹ������
struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

//��ȡ��������Ӱ����
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{        
    DirectionalShadowData data;
    //data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    //��ȡ�ƹ�ķ���ƫ��ֵ
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}
//��ȡ�������͹�Դ����Ӱ����
OtherShadowData GetOtherShadowData(int lightIndex)
{
    OtherShadowData data;
    data.strength = _OtherLightShadowData[lightIndex].x;
    data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
    return data;
}

//��ȡ����������
int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

//��ȡָ�������ķ���������
Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData)
{
    shadowData = UpdateShadowDataCascade(surfaceWS, _DirectionalLightShadowData[index].y, shadowData);
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    //�õ���Ӱ����
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
    //�õ���Ӱ˥��
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
    return light;
}

//��ȡָ�������ķǶ����Դ����
Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData) 
{
    Light light;
    light.color = _OtherLightColors[index].rgb;
    float3 ray = _OtherLightPositions[index].xyz - surfaceWS.position;
    light.direction = normalize(ray);
    //����ǿ�������˥��
    float distanceSqr = max(dot(ray, ray), 0.00001);
    //���ù�ʽ��������շ�Χ˥��
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w)));
    float4 spotAngles = _OtherLightSpotAngles[index];
    //����۹��˥��ֵ
    float spotAttenuation = Square(saturate(dot(_OtherLightDirections[index].xyz, light.direction) * spotAngles.x + spotAngles.y));
    OtherShadowData otherShadowData = GetOtherShadowData(index);
    //����ǿ���淶Χ�;���˥��
    light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS) * spotAttenuation * rangeAttenuation / distanceSqr;
    return light;
}

//��ȡ�Ƕ����Դ������
int GetOtherLightCount()
{
    return _OtherLightCount;
}
 
#endif