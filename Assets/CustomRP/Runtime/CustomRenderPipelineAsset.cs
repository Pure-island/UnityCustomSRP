using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    //����һ��RenderPipelineʵ��    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline();
    }
}
