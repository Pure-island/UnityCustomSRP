using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class Lighting 
{
    const string bufferName = "Lighting";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    //�������ɼ�ƽ�й�����Ϊ4
    const int maxDirLightCount = 4;
    //�����������͹�Դ���������
    const int maxOtherLightCount = 64;

    //static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    //static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionsAndMasksId = Shader.PropertyToID("_DirectionalLightDirectionsAndMasks");
    static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
    static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
    static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
    static int otherLightDirectionsAndMasksId = Shader.PropertyToID("_OtherLightDirectionsAndMasks");
    static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");

    //�洢�������͹�Դ����ɫ��λ������
    static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightDirectionsAndMasks = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];

    //�洢�ɼ������ɫ�ͷ���
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirectionsAndMasks = new Vector4[maxDirLightCount];

    CullingResults cullingResults;

    Shadows shadows = new Shadows();
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    //�洢��Ӱ����
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];
    static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");
    Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];

    static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings, bool useLightsPerObject, int renderingLayerMask)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        shadows.Setup(context, cullingResults, shadowSettings);
        //���͹�Դ����
        SetupLights(useLightsPerObject, renderingLayerMask);
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //���Ͷ����Դ����
    void SetupLights(bool useLightsPerObject, int renderingLayerMask)
    {
        //�õ���Դ�����б�
        NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
        //�õ����пɼ���
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0, otherLightCount = 0;
        int i;
        for (i = 0; i < visibleLights.Length; i++)
        {
            int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];
            Light light = visibleLight.light;
            if ((light.renderingLayerMask & renderingLayerMask) != 0)
            {
                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (dirLightCount < maxDirLightCount)
                        {
                            SetupDirectionalLight(dirLightCount++, i, ref visibleLight, light);
                        }
                        break;
                    case LightType.Point:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupPointLight(otherLightCount++, i, ref visibleLight, light);
                        }
                        break;
                    case LightType.Spot:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupSpotLight(otherLightCount++, i, ref visibleLight, light);
                        }
                        break;
                }
            }
            //ƥ���Դ����
            if (useLightsPerObject)
            {
                indexMap[i] = newIndex;
            }
        }
        //�������в��ɼ��������
        if (useLightsPerObject)
        {
            for (; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }

            cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();
            Shader.EnableKeyword(lightsPerObjectKeyword);
        }
        else
        {
            Shader.DisableKeyword(lightsPerObjectKeyword);
        }
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsAndMasksId, dirLightDirectionsAndMasks);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }

        buffer.SetGlobalInt(otherLightCountId, otherLightCount);
        if (otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsAndMasksId, otherLightDirectionsAndMasks);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
        }
    }

    //���ɼ���Ĺ�����ɫ�ͷ���洢������
    void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
    {
        dirLightColors[index] = visibleLight.finalColor;
        //ͨ��VisibleLight.localToWorldMatrix�����ҵ�ǰ��ʸ��,���ھ�������У���Ҫ����ȡ��
        Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
        dirLightDirectionsAndMasks[index] = dirAndMask;
        //�洢��Ӱ����
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(light, visibleIndex);
    }

    //�����Դ����ɫ��λ����Ϣ�洢������
    void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
    {
        otherLightColors[index] = visibleLight.finalColor;
        //λ����Ϣ�ڱ��ص������ת����������һ��
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        //�����շ�Χ��ƽ���ĵ����洢�ڹ�Դλ�õ�W������
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        otherLightSpotAngles[index] = new Vector4(0f, 1f);
        Vector4 dirAndMask = Vector4.zero;
        dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
        otherLightDirectionsAndMasks[index] = dirAndMask;
        //Light light = visibleLight.light;
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }

    //���۹�ƹ�Դ����ɫ��λ�úͷ�����Ϣ�洢������
    void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        //���ص������ת������ĵ��������󷴵õ����շ���
        otherLightDirectionsAndMasks[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
        otherLightDirectionsAndMasks[index] = dirAndMask;
        //Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }

    //�ͷ���Ӱ��ͼRT�ڴ�
    public void Cleanup()
    {
        shadows.Cleanup();
    }

}

public static class ReinterpretExtensions
{
    [StructLayout(LayoutKind.Explicit)]
    struct IntFloat
    {
        [FieldOffset(0)]
        public int intValue;
        [FieldOffset(0)]
        public float floatValue;
    }

    public static float ReinterpretAsFloat(this int value)
    {
        IntFloat converter = default;
        converter.intValue = value;
        return converter.floatValue;
    }
}