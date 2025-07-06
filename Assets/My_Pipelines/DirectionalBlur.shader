Shader "Hidden/Shader/DirectionalBlur"
{
    Properties
    {
        // This property is necessary to make the CommandBuffer.Blit bind the source texture to _MainTex
        _MainTex("Input Frame Buffer", 2DArray) = "grey" {}
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

    int _Horizontal; // 1 for horizontal blur, 0 for vertical blur
    TEXTURE2D_X(_MainTex);

    #define TEXTURE2D_SAMPLE_FUNCTION(uv) \
        SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, uv)

    #include "GaussBlur.hlsl"

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // Note that if HDUtils.DrawFullScreen is not used to render the post process, you don't need to call ClampAndScaleUVForBilinearPostProcessTexture.
        float2 uv = ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy);

    #if !__INTERNAL_IS_BYPASS

        // float3 color = GaussBlur(_Horizontal == 1, uv);
        float3 color = GaussianBlurSingle(uv).xyz;

    #else  // __INTERNAL_IS_BYPASS

        // Bypass the effect if needed
        float3 color = TEXTURE2D_SAMPLE_FUNCTION(uv);

    #endif  // !__INTERNAL_IS_BYPASS

        return float4(color, 1);
    }

    #undef TEXTURE2D_SAMPLE_FUNCTION
    #undef __INTERNAL_IS_BYPASS

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "CustomBloomEffect"

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
