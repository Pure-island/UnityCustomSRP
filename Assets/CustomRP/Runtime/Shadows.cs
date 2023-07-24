using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    const string bufferName = "Shadows";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    CullingResults cullingResults;

    ShadowSettings settings;

    //��Ͷ����Ӱ�Ķ��������
    const int maxShadowedDirectionalLightCount = 4;
    //���������
    const int maxCascades = 4;

    //�Ѵ洢�Ŀ�Ͷ����Ӱ��ƽ�й�����
    int ShadowedDirectionalLightCount;

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        //б�ȱ���ƫ��ֵ
        public float slopeScaleBias;
        //��Ӱ��׵����ü�ƽ��ƫ��
        public float nearPlaneOffset;
    }
    //�洢��Ͷ����Ӱ�Ŀɼ���Դ������
    ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    //��Դ����Ӱת������
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

    static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
    static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades * maxShadowedDirectionalLightCount];

    //��������
    static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    static Vector4[] cascadeData = new Vector4[maxCascades * maxShadowedDirectionalLightCount];

    //static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");
    static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

    static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");

    bool useShadowMask;
    static string[] shadowMaskKeywords =
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };

    //PCF�˲�ģʽ
    static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };
    static string[] cascadeBlendKeywords = { "_CASCADE_BLEND_SOFT", "_CASCADE_BLEND_DITHER" };

    //���ùؼ��ֿ�������PCF�˲�ģʽ
    void SetKeywords(string[] keywords, int enabledIndex)
    {
        // int enabledIndex = (int)settings.directional.filter - 1;
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    //�洢�ɼ������Ӱ����
    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //�洢�ɼ���Դ��������ǰ���ǹ�Դ��������ӰͶ�䲢����Ӱǿ�Ȳ���Ϊ0 
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f )
        {
            float maskChannel = -1;
            //���ʹ����ShadowMask
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }
            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };
            //������Ӱǿ�Ⱥ���Ӱͼ�������
            return new Vector4(light.shadowStrength, settings.directional.cascadeCount * ShadowedDirectionalLightCount++, light.shadowNormalBias, maskChannel);
        }
        return new Vector4(0f, 0f, 0f, -1f);
    }

    public void Setup(
        ScriptableRenderContext context, CullingResults cullingResults,
        ShadowSettings settings
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;

        ShadowedDirectionalLightCount= 0;
        useShadowMask = false;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //��Ӱ��Ⱦ
    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        //�Ƿ�ʹ����Ӱ�ɰ�
        buffer.BeginSample(bufferName);
        SetKeywords(shadowMaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    //��Ⱦ�������Ӱ
    void RenderDirectionalShadows()
    {
        //����renderTexture����ָ������������Ӱ��ͼ
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //ָ����Ⱦ���ݴ洢��RT��
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //�����Ȼ�����
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        //Ҫ�ָ��ͼ�������ʹ�С
        int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        //�������з������Ⱦ��Ӱ
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        //�����������Ͱ�Χ�����ݷ��͵�GPU
        buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        //�������ݷ���GPU
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        //��Ӱת��������GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        //�����Ӱ�������Ӱ���ɾ��뷢��GPU
        //buffer.SetGlobalFloat(shadowDistanceId, settings.maxDistance);
        float f = 1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
        //���ùؼ���
        SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);
        //����ͼ����С�����ش�С
        buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    //��Ⱦ������Դ��Ӱ
    void RenderDirectionalShadows(int index, int split ,int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        //�õ�������Ӱ��ͼ��Ҫ�Ĳ���
        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.directional.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            //�޳�ƫ��
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = splitData;
            //����ͼ�������������ڹ�Դ��ͼ��ƫ�Ƽ��ϼ���������
            int tileIndex = tileOffset + i;
            //���ü�������
            SetCascadeData(tileIndex, splitData.cullingSphere, tileSize);

            //������Ⱦ�ӿ�
            //SetTileViewport(index, split, tileSize);
            //ͶӰ���������ͼ���󣬵õ�������ռ䵽�ƹ�ռ��ת������
            //dirShadowMatrices[index] = projectionMatrix * viewMatrix;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            
            //������ͼͶӰ����
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //����б�ȱ���ƫ��ֵ
            buffer.SetGlobalDepthBias(0, light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
        
    }

    //���ü�������
    void SetCascadeData(int tileIndex, Vector4 cullingSphere, float tileSize)
    {
        //��Χ��ֱ��������Ӱͼ��ߴ�=���ش�С
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)settings.directional.filter + 1f);
        cullingSphere.w -= filterSize;
        //�õ��뾶��ƽ��ֵ
        cullingSphere.w *= cullingSphere.w;        
        cascadeCullingSpheres[tileIndex] = cullingSphere;
        cascadeData[tileIndex] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
    }

    //������Ⱦ�ӿ�����Ⱦ����ͼ��
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        //��������ͼ���ƫ��λ��
        Vector2 offset = new Vector2(index % split, index / split);
        //������Ⱦ�ӿڣ���ֳɶ��ͼ��
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    //����һ��������ռ�ת����Ӱ����ͼ��ռ�ľ���
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        //���ʹ���˷���Zbuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //���þ�������
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }

    //�ͷ���ʱ��Ⱦ����
    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }
}
