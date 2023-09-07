using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    //是否使用逐对象光照
    [SerializeField]
    bool useLightsPerObject = true;
    [SerializeField]
    ShadowSettings shadows = default;
    //后效资产配置
    [SerializeField]
    PostFXSettings postFXSettings = default;
    //HDR设置
    [SerializeField]
    bool allowHDR = true;
    public enum ColorLUTResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    }
    //LUT分辨率 
    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    //返回一个RenderPipeline实例    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(allowHDR, useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSettings, (int)colorLUTResolution);
    }
}
