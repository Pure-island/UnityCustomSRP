using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer 
{
    private partial void DrawUnsupportedShaders();

    private partial void DrawGizmos();

    private partial void PrepareForSceneWindow();

    private partial void PrepareBuffer();

#if UNITY_EDITOR
    static ShaderTagId[] legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
    };

    static Material errorMaterial;
    //绘制SRP不支持的着色器类型
    private partial void DrawUnsupportedShaders()
    {
        if(errorMaterial == null)
        {
            //不支持的ShaderTag类型，使用错误材质shader
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        //数组的第一个元素在构造drawingSettings时设置好了
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)) { overrideMaterial = errorMaterial };
        //遍历数组逐个设置着色器的PassName，从1开始
        for(int i = 1;i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        //使用默认设置
        var filteringSettings = FilteringSettings.defaultValue;
        //绘制不支持的shaderTag类型的物体
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
    }

    private partial void DrawGizmos()
    {
        if(Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    //在game视图绘制的几何体也绘制到Scene视图中
    private partial void PrepareForSceneWindow()
    {
        if(camera.cameraType == CameraType.SceneView)
        {
            //如果切换到了Scene视图，调用此方法完成绘制
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera); 
        }
    }

    string SampleName { get; set; }
    private partial void PrepareBuffer()
    {
        //设置只在编辑器模式下才分配内存
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
#else
    const string SampleName = BufferName;
#endif
}
