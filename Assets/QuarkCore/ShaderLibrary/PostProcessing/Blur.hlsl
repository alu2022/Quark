#ifndef _QUARK_BLUR_
#define _QUARK_BLUR_

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
float _BlurRange;
CBUFFER_END

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct appdata
{
    half4 positionOS : POSITION;
    half2 uv : TEXCOORD0;
};

struct v2f
{
    half4 vertex : SV_POSITION;
    half2 uv : TEXCOORD0;
};

v2f PostProcessingVert(appdata v)
{
    v2f o = (v2f) 0;
    o.vertex = TransformObjectToHClip(v.positionOS.xyz);
    o.uv = v.uv;
    return o;
}

half4 BoxBlurFrag(v2f i) : SV_Target
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

half4 GaussianBlurFragHor(v2f i) : SV_Target
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

half4 GaussianBlurFragVert(v2f i) : SV_Target
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

half4 KawaseBlurFrag(v2f i) : SV_Target
{
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-1, -1) * _BlurRange);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(1, -1) * _BlurRange);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-1, 1) * _BlurRange);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(1, 1) * _BlurRange);
    col /= 5;
    return col;
}

half4 DualKawaseDownFrag(v2f i) : SV_Target
{
    float halfBlurRange = _BlurRange * 0.5;
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * 4;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-halfBlurRange, -halfBlurRange));
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-halfBlurRange, halfBlurRange));
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(halfBlurRange, -halfBlurRange));
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(halfBlurRange, halfBlurRange));

    return col * 0.125;
}

half4 DualKawaseUpFrag(v2f i) : SV_Target
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