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
    //返回一个RenderPipeline实例    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows);
    }
}
