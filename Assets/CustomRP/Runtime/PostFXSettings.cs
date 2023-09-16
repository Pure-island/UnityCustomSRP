using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [SerializeField]

    Shader shader = default;

    [System.NonSerialized]

    Material material;

    public Material Material
    {
        get
        {
            if (material == null && shader != null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }
    

    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)]
        public int maxIterations;

        [Min(1f)]
        public int downscaleLimit;

        public bool bicubicUpsampling;

        [Min(0f)]
        public float threshold;

        [Range(0f, 1f)]
        public float thresholdKnee;

        [Min(0f)]
        public float intensity;

        //淡化闪烁 
        public bool fadeFireflies;

        public enum Mode { Additive, Scattering }
        public Mode mode;
        [Range(0.05f, 0.95f)]
        public float scatter;

        //是否忽略渲染缩放
        public bool ignoreRenderScale;
    }
    [SerializeField]

    BloomSettings bloom = new BloomSettings
    {
        scatter = 0.7f
    };

    public BloomSettings Bloom => bloom;

    [System.Serializable]

    public struct ToneMappingSettings
    {
        public enum Mode {            
            Reinhard,
            Neutral,
            ACES,
            None
        }
        public Mode mode;

    }
    [SerializeField]

    ToneMappingSettings toneMapping = default;
    public ToneMappingSettings ToneMapping => toneMapping;

    [Serializable]

    public struct ColorAdjustmentsSettings
    {
        //后曝光，调整场景的整体曝光度
        public float postExposure;
        //对比度，扩大或缩小色调值的总体范围
        [Range(-100f, 100f)]
        public float contrast;
        //颜色滤镜，通过乘以颜色来给渲染器着色  
        [ColorUsage(false, true)]
        public Color colorFilter;
        //色调偏移，改变所有颜色的色调
        [Range(-180f, 180f)]
        public float hueShift;
        //饱和度，推动所有颜色的强度
        [Range(-100f, 100f)]
        public float saturation;
    }

    [SerializeField]
    ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings
    {
        colorFilter = Color.white
    };

    public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;

    [Serializable]
    public struct WhiteBalanceSettings
    {
        //色温，调整白平衡的冷暖偏向
        [Range(-100f, 100f)]
        public float temperature;
        //色调，调整温度变化后的颜色
        [Range(-100f, 100f)]
        public float tint;
    }

    [SerializeField]
    WhiteBalanceSettings whiteBalance = default;
    public WhiteBalanceSettings WhiteBalance => whiteBalance;

    [Serializable]

    public struct SplitToningSettings
    {
        //用于对阴影和高光着色
        [ColorUsage(false)]
        public Color shadows, highlights;
        //设置阴影和高光之间的平衡的滑块
        [Range(-100f, 100f)]
        public float balance;

    }

    [SerializeField]
    SplitToningSettings splitToning = new SplitToningSettings
    {
        shadows = Color.gray,
        highlights = Color.gray
    };

    public SplitToningSettings SplitToning => splitToning;

    [Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 red, green, blue;
    }


    [SerializeField]
    ChannelMixerSettings channelMixer = new ChannelMixerSettings
    {
        red = Vector3.right,
        green = Vector3.up,
        blue = Vector3.forward
    };

    public ChannelMixerSettings ChannelMixer => channelMixer;


    [Serializable]
    public struct ShadowsMidtonesHighlightsSettings
    {
        [ColorUsage(false, true)]
        public Color shadows, midtones, highlights;

        [Range(0f, 2f)]
        public float shadowsStart, shadowsEnd, highlightsStart, highLightsEnd;
    }

    [SerializeField]

    ShadowsMidtonesHighlightsSettings
     shadowsMidtonesHighlights = new ShadowsMidtonesHighlightsSettings
     {
         shadows = Color.white,
         midtones = Color.white,
         highlights = Color.white,
         shadowsEnd = 0.3f,
         highlightsStart = 0.55f,
         highLightsEnd = 1f
     };

    public ShadowsMidtonesHighlightsSettings ShadowsMidtonesHighlights => shadowsMidtonesHighlights;
}
