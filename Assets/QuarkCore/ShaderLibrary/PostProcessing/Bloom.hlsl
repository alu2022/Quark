#ifndef _QUARK_BLOOM_
#define _QUARK_BLOOM_

#include "Blur.hlsl"

CBUFFER_START(UnityPerMaterial)
    float _Threshold;
    float _Intensity;
CBUFFER_END

TEXTURE2D(_BloomTex);SAMPLER(sampler_BloomTex);

half4 BrightnessFrag(blur_v2f i) : SV_Target
{
    float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float luminance = saturate(max(max(color.r,color.g),color.b) - _Threshold);
    //float luminance = saturate(Luminance(color.rgb) - _Threshold);
    
    return color * luminance;
}

half4 MixFrag(blur_v2f i) : SV_Target
{
    float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float4 bloomTexColor = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.uv);
    
    return mainTexColor + bloomTexColor * _Intensity;
}

#endif