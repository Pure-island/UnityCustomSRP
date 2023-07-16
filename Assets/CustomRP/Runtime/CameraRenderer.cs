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

        PrepareBuffer();        //设置SampleName
        PrepareForSceneWindow();//绘制UI
        if (!Cull()) return;    //裁剪并将结果存入cullingResults
        Setup();                //初始化
        lighting.Setup(context, cullingResults);//设置照明
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing);  //绘制可见物体
        DrawUnsupportedShaders();//绘制SRP不支持的着色器类型
        DrawGizmos();           //绘制Gizmos
        Submit();               //提交
    }



    private bool Cull()
    {
        ScriptableCullingParameters p;
        if(camera.TryGetCullingParameters(out p))   //得到需要进行剔除检查的所有物体
        {
            cullingResults = context.Cull(ref p);   //剔除
            return true;
        }
        return false;
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);              //设置相机属性
        CameraClearFlags flags = camera.clearFlags;         //得到相机的clear flags
        //设置相机清除状态
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

    //提交缓冲区渲染命令
    private void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    //绘制可见物
    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        //设置渲染相机的绘制顺序（远到近）
        var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        //设置shader pass和排序模式
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        //只绘制不透明物体，render queue在0-2500
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        //1.不透明物体绘制
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        //2.天空盒子绘制        
        context.DrawSkybox(camera);

        //设置渲染相机的绘制顺序（近到远）
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        //只绘制透明物体，render queue在2501-5000
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //3.透明物体绘制
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

}
