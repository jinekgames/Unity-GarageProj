Shader "Hidden/Shader/BlendBloomShader"
{
    Properties
    {
        _OrigTex("Original Texture", 2DArray) = "black" {}
        _BloomTex("Bloom Texture", 2DArray) = "white" {}
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    #include "FrameBufferData.hlsl"
    #include "FrameBufferVert.hlsl"

    // List of properties to control your post process effect
    TEXTURE2D_X(_OrigTex);
    TEXTURE2D_X(_BloomTex);

    float _BloomSaturation    = 1.0;
    float _BloomIntensity     = 1.0;
    float _OriginalSaturation = 1.0;
    float _OriginalIntensity  = 1.3;

    float AdjustSaturation(float3 Color, float Saturation)
    {
        float Grey = dot(Color, float3(0.3, 0.59, 0.11));
        return lerp(Grey, dot(Color, Color), Saturation);
        return Color;
    }

    float3 Blend(float3 origColor, float3 bloomColor)
    {
        float bloomSat = AdjustSaturation(bloomColor, _BloomSaturation)    * _BloomIntensity;
        float origSat  = AdjustSaturation(origColor,  _OriginalSaturation) * _OriginalIntensity;
        origColor *= (1 - saturate(bloomSat));
        return origColor + bloomColor;
    }

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // Note that if HDUtils.DrawFullScreen is not used to render the post process, you don't need to call ClampAndScaleUVForBilinearPostProcessTexture.

        float3 origColor  = SAMPLE_TEXTURE2D_X(_OrigTex,  s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy)).xyz;
        float3 bloomColor = SAMPLE_TEXTURE2D_X(_BloomTex, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy)).xyz;

        return float4(Blend(origColor, bloomColor), 1.0f);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "BlendBloonShader"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
