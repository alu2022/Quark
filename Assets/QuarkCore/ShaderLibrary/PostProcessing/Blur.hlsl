#ifndef _QUARK_BLUR_
#define _QUARK_BLUR_

#include "Common.hlsl"

CBUFFER_START(UnityPerMaterial)
float _BlurRange;
float _Iteration;
CBUFFER_END

struct blur_appdata
{
    half4 positionOS : POSITION;
    half2 uv : TEXCOORD0;
};

struct blur_v2f
{
    half4 vertex : SV_POSITION;
    half2 uv : TEXCOORD0;
};

blur_v2f PostProcessingVert(blur_appdata v)
{
    blur_v2f o = (blur_v2f) 0;
    o.vertex = TransformObjectToHClip(v.positionOS.xyz);
    o.uv = v.uv;
    return o;
}

half4 BoxBlurFrag(blur_v2f i) : SV_Target
{
    float3 color = float3(0, 0, 0);
    
    for (int idx1 = -1; idx1 <= 1; ++idx1)
    {
        for (int idx2 = -1; idx2 <= 1; ++idx2)
        {
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(idx1, idx2) * _BlurRange);
        }
    }
    
    color /= 9;
    return half4(color, 1);
}

float Rand(float2 n)
{
    return sin(dot(n, half2(1233.224, 1743.335)));
}

half4 GrainyBlurFrag(blur_v2f i) : SV_Target
{
    half2 randomOffset = float2(0.0, 0.0);
    half4 color = half4(0.0, 0.0, 0.0, 0.0);
    float random = Rand(i.uv);
		
    for (int k = 0; k < int(_Iteration); k++)
    {
        random = frac(43758.5453 * random + 0.61432);;
        randomOffset.x = (random - 0.5) * 2.0;
        random = frac(43758.5453 * random + 0.61432);
        randomOffset.y = (random - 0.5) * 2.0;
			
        color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, half2(i.uv + randomOffset * _BlurRange));
    }
    return color / _Iteration;
}

half4 RadialBlurFrag(blur_v2f i) : SV_Target
{
    half2 moveVec = (half2(0.5, 0.5) - i.uv) * _BlurRange;
    half4 color = half4(0, 0, 0, 0);
		
    [unroll(30)]
    for (int k = 0; k < int(_Iteration); k++)
    {
        color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
        i.uv += moveVec;
    }
    return color / _Iteration;
}

half4 GaussianBlurFragHor(blur_v2f i) : SV_Target
{
    float4 col = float4(0, 0, 0, 0);

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * 0.324f;
    
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(_BlurRange, 0.0)) * 0.232f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-_BlurRange, 0.0)) * 0.232f;
    
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(_BlurRange * 2, 0.0)) * 0.0855f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-_BlurRange * 2, 0.0)) * 0.0855f;
    
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(_BlurRange * 3, 0.0)) * 0.0205f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-_BlurRange * 3, 0.0)) * 0.0205f;

    return col;
}

half4 GaussianBlurFragVert(blur_v2f i) : SV_Target
{
    float4 col = float4(0, 0, 0, 0);

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * 0.324f;
    
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0.0, _BlurRange)) * 0.232f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0.0, -_BlurRange)) * 0.232f;
    
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0.0, _BlurRange * 2)) * 0.0855f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0.0, -_BlurRange * 2)) * 0.0855f;
    
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0.0, _BlurRange * 3)) * 0.0205f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0.0, - _BlurRange * 3)) * 0.0205f;

    return col;
}

half4 KawaseBlurFrag(blur_v2f i) : SV_Target
{
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-1, -1) * _BlurRange);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(1, -1) * _BlurRange);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-1, 1) * _BlurRange);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(1, 1) * _BlurRange);
    col /= 5;
    return col;
}

half4 DualKawaseDownFrag(blur_v2f i) : SV_Target
{
    float halfBlurRange = _BlurRange * 0.5;
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * 4;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-halfBlurRange, -halfBlurRange));
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-halfBlurRange, halfBlurRange));
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(halfBlurRange, -halfBlurRange));
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(halfBlurRange, halfBlurRange));

    return col * 0.125;
}

half4 DualKawaseUpFrag(blur_v2f i) : SV_Target
{
    float halfBlurRange = _BlurRange * 0.5;
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * 2;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-halfBlurRange, halfBlurRange)) * 2;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(halfBlurRange, -halfBlurRange)) * 2;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(halfBlurRange, halfBlurRange)) * 2;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-_BlurRange, 0));
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, -_BlurRange));
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(_BlurRange, 0));
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, _BlurRange));

    return col * 0.0833;
}

#endif