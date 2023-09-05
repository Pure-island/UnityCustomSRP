using System;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer 
{
    ScriptableRenderContext context;
    Camera camera;

    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName,
    };

    CullingResults cullingResults;
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    Lighting lighting = new Lighting();
    PostFXStack postFXStack = new PostFXStack();
    bool useHDR;

    public void Render(ScriptableRenderContext context, Camera camera, bool allowHDR, bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();        //����SampleName
        PrepareForSceneWindow();//����UI
        if (!Cull(shadowSettings.maxDistance)) return;    //�ü������������cullingResults
        useHDR = allowHDR && camera.allowHDR;
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings, useLightsPerObject);//��������
        postFXStack.Setup(context, camera, postFXSettings, useHDR);//���ú���
        buffer.EndSample(SampleName);
        Setup();                                                //��ʼ��                        
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing, useLightsPerObject);  //���ƿɼ�����
        DrawUnsupportedShaders();//����SRP��֧�ֵ���ɫ������
        DrawGizmosBeforeFX();          //����Gizmos
        if (postFXStack.IsActive)
        {
            postFXStack.Render(frameBufferId);
        }
        DrawGizmosAfterFX();
        Cleanup();
        Submit();               //�ύ
    }

    private void Cleanup()
    {
        lighting.Cleanup();

        if (postFXStack.IsActive)
        {
            buffer.ReleaseTemporaryRT(frameBufferId);
        }
    }

    private bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;
        if(camera.TryGetCullingParameters(out p))   //�õ���Ҫ�����޳�������������
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);   //�޳�
            return true;
        }
        return false;
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);              //�����������
        CameraClearFlags flags = camera.clearFlags;         //�õ������clear flags
        if (postFXStack.IsActive)
        {
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }
            buffer.GetTemporaryRT(frameBufferId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            buffer.SetRenderTarget(frameBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }

        //����������״̬
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth,
                                flags == CameraClearFlags.Color,
                                flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear); 
        buffer.BeginSample(SampleName);        
        ExecuteBuffer();        
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //�ύ��������Ⱦ����
    private void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    //���ƿɼ���
    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject)
    {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        //������Ⱦ����Ļ���˳��Զ������
        var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        //����shader pass������ģʽ
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
            PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes | lightsPerObjectFlags
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        //ֻ���Ʋ�͸�����壬render queue��0-2500
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        //1.��͸���������
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        //2.��պ��ӻ���        
        context.DrawSkybox(camera);

        //������Ⱦ����Ļ���˳�򣨽���Զ��
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        //ֻ����͸�����壬render queue��2501-5000
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //3.͸���������
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

}
