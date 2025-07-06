Shader "Hidden/Shader/LightFilter"
{
    Properties
    {
        // This property is necessary to make the CommandBuffer.Blit bind the source texture to _MainTex
        _MainTex("Main Texture", 2DArray) = "black" {}
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    #include "FrameBufferData.hlsl"
    #include "FrameBufferVert.hlsl"

    #define __INTERNAL_IS_BYPASS false

    // List of properties to control your post process effect
    float _Threshold = 0.5;
    TEXTURE2D_X(_MainTex);

    float CalculateLuminance(float3 color)
    {
        // Calculate luminance using the Rec. 709 formula
        return dot(color, float3(0.2126, 0.7152, 0.0722));
    }

    bool FilterBrightness(float3 color, float threshold)
    {
    #if !__INTERNAL_IS_BYPASS
        // Example condition to filter brightness
        return CalculateLuminance(color) > threshold; // Adjust threshold as needed
    #else  // __INTERNAL_IS_BYPASS
        return true; // Always return true to bypass filtering
    #endif  // !__INTERNAL_IS_BYPASS
    }

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // Note that if HDUtils.DrawFullScreen is not used to render the post process, you don't need to call ClampAndScaleUVForBilinearPostProcessTexture.

        float2 uv          = ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy);
        float3 sourceColor = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, uv).xyz;

        if (!FilterBrightness(sourceColor, _Threshold))
            return float4(0,0,0,1);
        return float4(sourceColor, 1);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "LightFilter"

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
