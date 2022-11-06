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

#endif