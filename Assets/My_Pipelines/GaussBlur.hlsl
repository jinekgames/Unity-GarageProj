#define __GAUSS_BETTER_MODEL
#define GAUSSIAN_BLUR_UNROLL 1

#if defined(__GAUSS_BETTER_MODEL)
// https://github.com/aki-null/GaussianBlurURP/blob/main/GaussianBlur.hlsl

inline float Gauss(float sigma, int x)
{
    return exp(-(x * x) / (2 * sigma * sigma));
}

inline float Gauss(float sigma, int x, bool isHalf)
{
    return Gauss(sigma, x) * (isHalf ? 0.5 : 1);
}

inline float GaussianWeightSum1D(float sigma, int radius)
{
    float sum = 0;
    #if GAUSSIAN_BLUR_UNROLL
    UNITY_UNROLL
    #endif
    for (int i = 0; i < radius * 2 + 1; i++)
    {
        sum += Gauss(sigma, i - radius);
    }
    return sum;
}

inline float GaussianWeightSum2D(float sigma, int radius)
{
    const float baseSum = GaussianWeightSum1D(sigma, radius);
    float sum = 0;
    #if GAUSSIAN_BLUR_UNROLL
    UNITY_UNROLL
    #endif
    for (int i = 0; i < radius * 2 + 1; i++)
    {
        sum += Gauss(sigma, i - radius) * baseSum;
    }
    return sum;
}

// Separable Gaussian blur function with configurable sigma and radius.
//
// The delta parameter should be (texelSize.x, 0), and (0, texelSize.y) for each passes.
//
float4 GaussBlurInternal(float2 uv, float2 delta, float sigma, int radius)
{
    int idx = -radius;
    float4 res = 0;

    const float totalWeightRcp = rcp(GaussianWeightSum1D(sigma, radius));

    // Exploit bilinear sampling to reduce the number of texture fetches, only requiring radius + 1 fetches.
    #if GAUSSIAN_BLUR_UNROLL
    UNITY_UNROLL
    #endif
    for (int i = 0; i < radius + 1; ++i)
    {
        const int x0 = idx;
        // Sample just the center texel if the radius is an even number
        const bool isNarrow = (radius & 1) == 0 && x0 == 0;
        const int x1 = isNarrow ? x0 : x0 + 1;

        // Calculate the weights for each texel
        const float w0 = Gauss(sigma, x0, x0 == 0);
        const float w1 = Gauss(sigma, x1, x1 == 0);

        // Adjust the sampling position depending on the required weight.
        // Use bilinear sampling to fetch two texels at once.
        const float texelOffset = isNarrow ? 0 : w1 / (w0 + w1);
        const float2 sampleUV = uv + (x0 + texelOffset) * delta;
        const float weight = (w0 + w1) * totalWeightRcp;
        res += TEXTURE2D_SAMPLE_FUNCTION(sampleUV) * weight;

        // Step to the next sample
        UNITY_FLATTEN
        if ((radius & 1) == 1 && x1 == 0)
        {
            idx = 0;
        }
        else
        {
            idx = x1 + 1;
        }
    }
    return res;
}

float4 GaussBlur(bool isHorizontal, float2 uv, float sigma = 5, int radius = 20)
{
    float2 delta = isHorizontal ? float2(1.0f/_ScreenParams.x, 0)
                                : float2(0, 1.0f/_ScreenParams.y);
    return GaussBlurInternal(uv, delta, sigma, radius);
}

float4 GaussianBlurSingle(float2 uv, float sigma = 5, int radius = 20)
{
    float2 delta = 1.0f / _ScreenParams;
    float4 res = 0;

    const float totalWeightRcp = rcp(GaussianWeightSum2D(sigma, radius));

    int idxY = -radius;
    #if GAUSSIAN_BLUR_UNROLL
    UNITY_UNROLL
    #endif
    for (int i = 0; i < radius + 1; ++i)
    {
        const int y0 = idxY;
        // Narrow state represents a flag where a single pixel is sampled instead of two
        const bool isNarrowY = (radius & 1) == 0 && y0 == 0 || // Even radius means center texel is sampled alone
            (radius & 1) == 1 && y0 == radius; // Odd radius means rightmost center is sampled alone
        const int y1 = isNarrowY ? y0 : y0 + 1;

        int idxX = -radius;

        #if GAUSSIAN_BLUR_UNROLL
        UNITY_UNROLL
        #endif
        for (int j = 0; j < radius + 1; ++j)
        {
            const int x0 = idxX;
            const bool isNarrowX = (radius & 1) == 0 && x0 == 0 || (radius & 1) == 1 && x0 == radius;
            const int x1 = isNarrowX ? x0 : x0 + 1;

            // Weights in both directions
            const float wx0 = Gauss(sigma, x0, isNarrowX);
            const float wx1 = Gauss(sigma, x1, isNarrowX);
            const float wy0 = Gauss(sigma, y0, isNarrowY);
            const float wy1 = Gauss(sigma, y1, isNarrowY);

            // Adjust the sampling position depending on the required weight.
            // Use bilinear sampling to fetch four texels at once if possible.
            const float2 texelOffset = float2(isNarrowX ? 0 : wx1 / (wx0 + wx1), isNarrowY ? 0 : wy1 / (wy0 + wy1));
            const float2 sampleUV = uv + (float2(x0, y0) + texelOffset) * delta;

            // Sum the weights of four texels, and normalize
            const float weight = ((wx0 + wx1) * wy0 + (wx0 + wx1) * wy1) * totalWeightRcp;
            res += TEXTURE2D_SAMPLE_FUNCTION(sampleUV) * weight;

            // Step to the next sample
            idxX = x1 + 1;
        }

        // Step to the next sample
        idxY = y1 + 1;
    }
    return res;
}

#endif  // __GAUSS_BETTER_MODEL

#if defined(__GAUSS_PRECALCAULATED_WEIGHT_MODEL)

#define _GAUSS_WEIGHT_COUNT 6

// Learn GL
#define _GAUSS_WEIGHT_5 { 0.227027f, 0.1945946f, 0.1216216f, 0.054054f, 0.016216f }
// Self calculated
#define _GAUSS_WEIGHT_6 { 0.564190f, 0.542067f, 0.480771f, 0.393622f, 0.297493f, 0.207554f }

#if (_GAUSS_WEIGHT_COUNT == 5)
#define _GAUSS_WEIGHT _GAUSS_WEIGHT_5
#elif (_GAUSS_WEIGHT_COUNT == 6)
#define _GAUSS_WEIGHT _GAUSS_WEIGHT_6
#endif  // (_GAUSS_WEIGHT_COUNT == ?)

float3 GaussBlur(bool isHorizontal, float2 uv)
{
    const float weight[_GAUSS_WEIGHT_COUNT] = _GAUSS_WEIGHT;

    float2 tex_offset = 1.0f / _ScreenParams.xy;
    float3 result = TEXTURE2D_SAMPLE_FUNCTION(uv) * weight[0];

    const float2 direction = isHorizontal ? float2(1.0f, 0.0f)
                                          : float2(0.0f, 1.0f);

    for(int i = 1; i < _GAUSS_WEIGHT_COUNT; ++i)
    {
        result += TEXTURE2D_SAMPLE_FUNCTION(uv + direction * tex_offset * i).xyz * weight[i];
        result += TEXTURE2D_SAMPLE_FUNCTION(uv - direction * tex_offset * i).xyz * weight[i];
    }

    return result;
}

#undef _GAUSS_WEIGHT
#undef _GAUSS_WEIGHT_6
#undef _GAUSS_WEIGHT_5
#undef _GAUSS_WEIGHT_COUNT

#endif  // defined(__GAUSS_PRECALCAULATED_WEIGHT_MODEL)
