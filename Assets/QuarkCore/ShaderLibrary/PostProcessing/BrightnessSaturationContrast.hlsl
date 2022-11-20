#ifndef _QUARK_BRISATCON_
#define _QUARK_BRISATCON_

#include "Common.hlsl"

CBUFFER_START(UnityPerMaterial)
    float _Brightness;
    float _Saturation;
    float _Contrast;
CBUFFER_END

struct brisatcon_appdata
{
    half4 positionOS : POSITION;
    half2 uv : TEXCOORD0;
};

struct brisatcon_v2f
{
    half4 vertex : SV_POSITION;
    half2 uv : TEXCOORD0;
};

brisatcon_v2f BriSatConVert(brisatcon_appdata v)
{
    brisatcon_v2f o = (brisatcon_v2f) 0;
    o.vertex = TransformObjectToHClip(v.positionOS.xyz);
    o.uv = v.uv;
    return o;
}

half4 BriSatConFrag(brisatcon_v2f i) : SV_Target
{
    float3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
    
    float luminance = Luminance(color);
    
    color = color * _Brightness;
    
    color = lerp(float3(luminance, luminance,luminance), color, _Saturation);
    
    color = lerp(float3(0.5, 0.5, 0.5), color, _Contrast);
    
    return half4(color.rgb, 1);
}

#endif