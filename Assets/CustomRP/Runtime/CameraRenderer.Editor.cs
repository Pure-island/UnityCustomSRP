using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer 
{
    partial void DrawUnsupportedShaders();

    // partial void DrawGizmos();
    partial void DrawGizmosBeforeFX();
    partial void DrawGizmosAfterFX();

    partial void PrepareForSceneWindow();

    partial void PrepareBuffer();

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
    //����SRP��֧�ֵ���ɫ������
    partial void DrawUnsupportedShaders()
    {
        if(errorMaterial == null)
        {
            //��֧�ֵ�ShaderTag���ͣ�ʹ�ô������shader
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        //����ĵ�һ��Ԫ���ڹ���drawingSettingsʱ���ú���
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)) { overrideMaterial = errorMaterial };
        //�����������������ɫ����PassName����1��ʼ
        for(int i = 1;i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        //ʹ��Ĭ������
        var filteringSettings = FilteringSettings.defaultValue;
        //���Ʋ�֧�ֵ�shaderTag���͵�����
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
    }

    //partial void DrawGizmos() 
    //{
    // if (Handles.ShouldRenderGizmos()) 
    // {
    // context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
    // context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
    // } 
    //}
    //����DrawGizmos

    partial void DrawGizmosBeforeFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
        }
    }

    partial void DrawGizmosAfterFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    //��game��ͼ���Ƶļ�����Ҳ���Ƶ�Scene��ͼ��
    partial void PrepareForSceneWindow()
    {
        if(camera.cameraType == CameraType.SceneView)
        {
            //����л�����Scene��ͼ�����ô˷�����ɻ���
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera); 
        }
    }

    string SampleName { get; set; }
    partial void PrepareBuffer()
    {
        //����ֻ�ڱ༭��ģʽ�²ŷ����ڴ�
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
#else
    const string SampleName = BufferName;
#endif
}
