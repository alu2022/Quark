#ifndef _QUARK_COMMON_
#define _QUARK_COMMON_

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
CBUFFER_END

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

 
float3 DecodeNormal(float4 enc)
{
    float kScale = 1.7777;
    float3 nn = enc.xyz * float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
    float g = 2.0 / dot(nn.xyz, nn.xyz);
    float3 n;
    n.xy = g * nn.xy;
    n.z = g - 1;
    return n;
}

float Luminance(float3 color)
{
    return 0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
}

#endif