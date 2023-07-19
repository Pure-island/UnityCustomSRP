//���������ؿ�
#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

//�õ������������
float3 GetLighting(Surface surfaceWS, BRDF brdf)
{
    //�õ�������Ӱ����
    ShadowData shadowData = GetShadowData(surfaceWS);
    //�ɼ���Ĺ��ս�������ۼӵõ����չ��ս��
    float3 color = 0.0;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
    }
    return color;
}
#endif