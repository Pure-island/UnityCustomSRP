using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();
    bool useDynamicBatching;
    bool useGPUInstancing;
    bool useLightsPerObject;
    ShadowSettings shadowSettings;

    //测试SRP合批启用
    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, bool useLightsPerObject, ShadowSettings shadowSettings)
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        //灯光使用线性强度
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        this.shadowSettings = shadowSettings;

        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras) 
        {
            renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings);
        }
    }
}

