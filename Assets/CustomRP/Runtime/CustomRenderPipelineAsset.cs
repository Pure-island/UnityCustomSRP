using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    //返回一个RenderPipeline实例    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline();
    }
}
