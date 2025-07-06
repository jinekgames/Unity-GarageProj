using System;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static Unity.VisualScripting.Member;

[Serializable, VolumeComponentMenu("Post-processing/Custom/CustomBloomEffect")]
public sealed class CustomBloomEffect : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls enabling of all.")]
    public BoolParameter isEnabled = new(true, true);
    [Tooltip("Controls enabling of filter stage.")]
    public BoolParameter enableFilter = new(true, true);
    [Tooltip("Controls enabling of horizontal blur stage.")]
    public BoolParameter enableBlurHorizontal = new(true, true);
    [Tooltip("Controls enabling of vertical blur stage.")]
    public BoolParameter enableBlurVertical = new(true, true);
    [Tooltip("Controls enabling of final blending stage.")]
    public BoolParameter enableFinalBlending = new(true, true);
    [Tooltip("Controls the brightness threshold.")]
    public FloatParameter brightnessThreshold = new (0.5f, true);
    [Tooltip("Bloom saturation.")]
    public FloatParameter bloomSaturation    = new(1.0f, true);
    [Tooltip("Bloom intensity.")]
    public FloatParameter bloomIntensity     = new(1.3f, true);
    [Tooltip("Original saturation.")]
    public FloatParameter originalSaturation = new(1.0f, true);
    [Tooltip("Original intensity.")]
    public FloatParameter originalIntensity  = new(1.0f, true);

    Material m_BrightnessFilterProgram = null;
    Material m_DirectionalBlurProgram  = null;
    Material m_CustomCopyShaderProgram = null;
    Material m_BlendBloomProgram       = null;

    public bool IsActive() => (m_DirectionalBlurProgram != null && m_BrightnessFilterProgram != null) &&
                              isEnabled.value;

    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Global Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    const string kShaderNameDirectionalBlur  = "Hidden/Shader/DirectionalBlur";
    const string kShaderNameBrightnessFilter = "Hidden/Shader/LightFilter";
    const string kShaderNameCustomCopyShader = "Hidden/Shader/CopyShader";
    const string kShaderNameBlendBloomShader = "Hidden/Shader/BlendBloomShader";

    public override void Setup()
    {
        m_DirectionalBlurProgram  = InitProgram(kShaderNameDirectionalBlur);
        m_BrightnessFilterProgram = InitProgram(kShaderNameBrightnessFilter);
        //m_CustomCopyShaderProgram = InitProgram(kShaderNameCustomCopyShader);
        m_BlendBloomProgram       = InitProgram(kShaderNameBlendBloomShader);

        //m_BlurOut_Horizontal = Tex.Create(100, 100, "BLOOM:BLUR_HORIZONTAL_OUT",
        //                                  GraphicsFormat.B10G11R11_UFloatPack32);
        //m_BlurOut_Vertical   = Tex.Create(100, 100, "BLOOM:BLUR_VERTICAL_OUT",
        //                                  GraphicsFormat.B10G11R11_UFloatPack32);
        //m_FilterOut          = Tex.Create(100, 100, "BLOOM:FILTER_OUT",
        //                                  GraphicsFormat.B10G11R11_UFloatPack32);
        //m_BlendOut           = Tex.Create(100, 100, "BLOOM:BLEND_OUT",
        //                                  GraphicsFormat.B10G11R11_UFloatPack32);
    }

    private Material InitProgram(string shaderName)
    {
        Material program = null;
        if (Shader.Find(shaderName) != null)
        {
            program = new Material(Shader.Find(shaderName));
        }
        else
        {
            Debug.LogError($"Unable to find shader '{shaderName}'. " +
                           "Post Process Volume CustomBloomEffect is unable to load.");
        }
        return program;
    }

    private RTHandle m_FilterOut          = null;
    private RTHandle m_BlurOut_Horizontal = null;
    private RTHandle m_BlurOut_Vertical   = null;
    private RTHandle m_BlendOut           = null;

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        // if (m_BlurOut_Vertical == null)
        //     m_BlurOut_Vertical = RTHandles.Alloc(source);
        // if (m_BlurOut_Horizontal == null)
        //     m_BlurOut_Horizontal = RTHandles.Alloc(source);
        // if (m_FilterOut == null)
        //     m_FilterOut = RTHandles.Alloc(source);
        // if (m_BlendOut == null)
        //     m_BlendOut = RTHandles.Alloc(source);

        Tex.Reallocate(ref m_BlurOut_Horizontal, camera.actualWidth, camera.actualHeight, "BLOOM:BLUR_HORIZONTAL_OUT",
                      Tex.GetGraphicsFormat(destination));
        Tex.Reallocate(ref m_BlurOut_Vertical,   camera.actualWidth, camera.actualHeight, "BLOOM:BLUR_VERTICAL_OUT",
                      Tex.GetGraphicsFormat(destination));
        Tex.Reallocate(ref m_FilterOut,          camera.actualWidth, camera.actualHeight, "BLOOM:FILTER_OUT",
                      Tex.GetGraphicsFormat(destination));
        Tex.Reallocate(ref m_BlendOut,            camera.actualWidth, camera.actualHeight, "BLOOM:BLEND_OUT",
                      Tex.GetGraphicsFormat(destination));

        // Debug.Log($"FILTER_OUT size: {m_FilterOut.rt.width}x{m_FilterOut.rt.height}");
        // Debug.Log($"BLUR_HORIZONTAL_OUT size: {m_BlurOut_Horizontal.rt.width}x{m_BlurOut_Horizontal.rt.height}");
        // Debug.Log($"BLUR_VERTICAL_OUT size: {m_BlurOut_Vertical.rt.width}x{m_BlurOut_Vertical.rt.height}");
        // Debug.Log($"BLEND_OUT size: {m_BlendOut.rt.width}x{m_BlendOut.rt.height}");

        var fence = cmd.CreateAsyncGraphicsFence();

        var resultTex = source;

        if (enableFilter.value)
        {
            resultTex = source;
            FilterBrightness(cmd, resultTex, m_FilterOut);
            resultTex = m_FilterOut;
            Sync(cmd, resultTex);
        }

        cmd.WaitOnAsyncGraphicsFence(fence);
        CopyTexture(cmd, resultTex, destination);

        if (enableBlurHorizontal.value)
        {
            BlurDirectional(cmd, resultTex, m_BlurOut_Horizontal, BlurDirection.Horizontal);
            resultTex = m_BlurOut_Horizontal;
            Sync(cmd, resultTex);
        }

        cmd.WaitOnAsyncGraphicsFence(fence);
        CopyTexture(cmd, resultTex, destination);

        if (enableBlurVertical.value)
        {
            BlurDirectional(cmd, resultTex, m_BlurOut_Vertical, BlurDirection.Vertical);
            resultTex = m_BlurOut_Vertical;
            Sync(cmd, resultTex);
        }

        cmd.WaitOnAsyncGraphicsFence(fence);
        CopyTexture(cmd, resultTex, destination);

        if (enableFinalBlending.value)
        {
            BlendBloom(cmd, source, resultTex, destination);
            resultTex = destination;
            Sync(cmd, resultTex);
        }

        cmd.WaitOnAsyncGraphicsFence(fence);
        CopyTexture(cmd, resultTex, destination);

        //Graphics.ExecuteCommandBuffer(cmd);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_DirectionalBlurProgram);
        CoreUtils.Destroy(m_BrightnessFilterProgram);
        //CoreUtils.Destroy(m_CustomCopyShaderProgram);

        Tex.Release(ref m_BlurOut_Horizontal);
        Tex.Release(ref m_BlurOut_Vertical);
        Tex.Release(ref m_FilterOut);
        Tex.Release(ref m_BlendOut);
    }

    class Tex
    {
        public static int GetWidth(RTHandle tex)
        {
            return tex.rt.width;
        }
        public static int GetHeight(RTHandle tex)
        {
            return tex.rt.height;
        }
        public static GraphicsFormat GetGraphicsFormat(RTHandle tex)
        {
            return tex.rt.graphicsFormat;
        }

        public static RTHandle Create(int width, int height, string _name,
                                      GraphicsFormat graphicsFormat)
        {
            RTHandle rtHandle = RTHandles.Alloc(
                width,
                height,
                1,
                colorFormat: graphicsFormat,
                name: _name,
                dimension: TextureDimension.Tex2DArray
            );
            return rtHandle;
        }

        public static void Release(ref RTHandle tex)
        {
            tex?.Release();
            tex = null;
        }

        public static bool Check(RTHandle tex, int width, int height, GraphicsFormat graphicsFormat)
        {
            return tex != null && width == GetWidth(tex) && height == GetHeight(tex) &&
                   graphicsFormat == GetGraphicsFormat(tex);
        }

        public static void Reallocate(ref RTHandle tex, int width, int height, string name,
                                      GraphicsFormat graphicsFormat)
        {
            RTHandles.SetReferenceSize(width, height);
            if (!Check(tex, width, height, graphicsFormat))
            {
                //if (tex == null)
                {
                    Release(ref tex);
                    Debug.Log($"Reallocating texture \"{name}\" ({width}x{height})");
                    tex = Create(width, height, name, graphicsFormat);
                }
            }
        }

        /*
        private static Texture2D CreateTexture2D(Vector2Int size, TextureFormat format = TextureFormat.RGBA32)
        {
            Texture2D texture = new Texture2D(size.x, size.y, format, false);
            texture.Apply();
            return texture;
        }
        Texture2DArray CreateTexture2DArray(Vector2Int size, int sliceCount, TextureFormat format = TextureFormat.RGBA32)
        {
            Texture2DArray texArray = new Texture2DArray(size.x, size.y, sliceCount, format, true);
            texArray.Apply();
            return texArray;
        }
        */
    }

    private enum BlurDirection
    {
        Horizontal,
        Vertical,
    }

    private void Sync(CommandBuffer cmd, RTHandle tex)
    {
        // TBD
    }
    private void CommandFinish(CommandBuffer cmd)
    {
         //Graphics.ExecuteCommandBuffer(cmd);
    }
    private void CopyTexture(CommandBuffer cmd, RTHandle source, RTHandle destination)
    {
        cmd.Blit(source, destination);

        CommandFinish(cmd);
    }
    private void BlurDirectional(CommandBuffer cmd, RTHandle source, RTHandle destination, BlurDirection direction)
    {
        if (m_DirectionalBlurProgram == null)
            return;

        m_DirectionalBlurProgram.SetTexture("_MainTex", source.rt);
        m_DirectionalBlurProgram.SetInt("_Horizontal", (direction == BlurDirection.Horizontal) ? 1 : 0);
        HDUtils.DrawFullScreen(cmd, m_DirectionalBlurProgram, destination, shaderPassId: 0);

        CommandFinish(cmd);
    }
    private void FilterBrightness(CommandBuffer cmd, RTHandle source, RTHandle destination)
    {
        if (m_BrightnessFilterProgram == null)
            return;

        m_BrightnessFilterProgram.SetTexture("_MainTex", source.rt);
        m_BrightnessFilterProgram.SetFloat("_Threshold", brightnessThreshold.value);
        HDUtils.DrawFullScreen(cmd, m_BrightnessFilterProgram, destination, shaderPassId: 0);

        CommandFinish(cmd);
    }
    private void BlendBloom(CommandBuffer cmd, RTHandle orig, RTHandle bloom, RTHandle destination)
    {
        if (m_BlendBloomProgram == null)
            return;

        m_BlendBloomProgram.SetTexture("_OrigTex", orig.rt);
        m_BlendBloomProgram.SetTexture("_BloomTex", bloom.rt);
        m_BrightnessFilterProgram.SetFloat("_BloomSaturation", bloomSaturation.value);
        m_BrightnessFilterProgram.SetFloat("_BloomIntensity", bloomIntensity.value);
        m_BrightnessFilterProgram.SetFloat("_OriginalSaturation", originalSaturation.value);
        m_BrightnessFilterProgram.SetFloat("_OriginalIntensity", originalIntensity.value);
        HDUtils.DrawFullScreen(cmd, m_BlendBloomProgram, destination, shaderPassId: 0);

        CommandFinish(cmd);
    }
}
