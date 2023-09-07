using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    //�Ƿ�ʹ����������
    [SerializeField]
    bool useLightsPerObject = true;
    [SerializeField]
    ShadowSettings shadows = default;
    //��Ч�ʲ�����
    [SerializeField]
    PostFXSettings postFXSettings = default;
    //HDR����
    [SerializeField]
    bool allowHDR = true;
    public enum ColorLUTResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    }
    //LUT�ֱ��� 
    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    //����һ��RenderPipelineʵ��    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(allowHDR, useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSettings, (int)colorLUTResolution);
    }
}
