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
    Lighting lighting = new Lighting();

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();        //����SampleName
        PrepareForSceneWindow();//����UI
        if (!Cull()) return;    //�ü������������cullingResults
        Setup();                //��ʼ��
        lighting.Setup(context, cullingResults);//��������
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing);  //���ƿɼ�����
        DrawUnsupportedShaders();//����SRP��֧�ֵ���ɫ������
        DrawGizmos();           //����Gizmos
        Submit();               //�ύ
    }



    private bool Cull()
    {
        ScriptableCullingParameters p;
        if(camera.TryGetCullingParameters(out p))   //�õ���Ҫ�����޳�������������
        {
            cullingResults = context.Cull(ref p);   //�޳�
            return true;
        }
        return false;
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);              //�����������
        CameraClearFlags flags = camera.clearFlags;         //�õ������clear flags
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
    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        //������Ⱦ����Ļ���˳��Զ������
        var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        //����shader pass������ģʽ
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
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
