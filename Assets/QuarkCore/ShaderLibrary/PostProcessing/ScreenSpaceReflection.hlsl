#ifndef _QUARK_ScreenSpaceReflection_
#define _QUARK_ScreenSpaceReflection_

#include "Common.hlsl"

#define SSRDitherMatrix_m0 float4(0,0.5,0.125,0.625)
#define SSRDitherMatrix_m1 float4(0.75,0.25,0.875,0.375)
#define SSRDitherMatrix_m2 float4(0.187,0.687,0.0625,0.562)
#define SSRDitherMatrix_m3 float4(0.937,0.437,0.812,0.312)

struct ssr_appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct ssr_v2f
{
    float2 uv : TEXCOORD0;
               
    float4 vertex : SV_POSITION;
    float4 rayVS : TEXCOORD1;
};

CBUFFER_START(UnityPerMaterial)       
    float _MaxStep;
    float _StepSize;
    float _MaxDistance;
    float _Thickness;
CBUFFER_END

TEXTURE2D(_CameraDepthNormalsTexture); 
SAMPLER(sampler_CameraDepthNormalsTexture);

inline float DecodeFloatRG(float2 enc)
{
    float2 kDecodeDot = float2(1.0, 1 / 255.0);
    return dot(enc, kDecodeDot);
}

inline float3 DecodeViewNormalStereo(float4 enc4)
{
    float kScale = 1.7777;
    float3 nn = enc4.xyz * float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
    float g = 2.0 / dot(nn.xyz, nn.xyz);
    float3 n;
    n.xy = g * nn.xy;
    n.z = g - 1;
    return n;
}

inline void DecodeDepthNormal(float4 enc, out float depth, out float3 normal)
{
    depth = DecodeFloatRG(enc.zw);
    normal = normalize(DecodeViewNormalStereo(enc));
}

ssr_v2f ScreenSpaceReflectionVert(ssr_appdata i)
{
    ssr_v2f o = (ssr_v2f) 0;
    o.vertex = TransformObjectToHClip(i.vertex);
    o.uv = i.uv;
    #if UNITY_UV_STARTS_TOP
        o.uv.y = 1 - o.uv.y;
    #endif
    float4 viewRayNDC = half4(i.uv * 2 - 1, 1, 1);
    float4 viewRayPS = viewRayNDC * _ProjectionParams.z;
    o.rayVS = mul(unity_CameraInvProjection, viewRayPS);
    return o;
}

half4 ScreenSpaceReflectionFrag(ssr_v2f i) : SV_Target
{
    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float2 reflectUV = 0;
    
    float4 depthNormals = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, i.uv);
    float linerDepth01 = 0;
    float3 normal = float3(0, 0, 0);
    DecodeDepthNormal(depthNormals, linerDepth01, normal);
    
    float3 posVS = i.rayVS.xyz * linerDepth01;
    
    float3 viewDir = normalize(posVS);
    float3 rayDir = reflect(viewDir, normal);
    
    float2 ditherXY = i.vertex.xy;
    float4x4 SSRDitherMatrix = float4x4(SSRDitherMatrix_m0, SSRDitherMatrix_m1, SSRDitherMatrix_m2, SSRDitherMatrix_m3);
    
    float2 XY = floor(fmod(ditherXY, 4));
    float dither = SSRDitherMatrix[XY.y][XY.x];
    
    UNITY_LOOP
    for (int i = 0; i < _MaxStep; ++i)
    {
        float3 raycastPosWS = posVS + rayDir * _StepSize * i + rayDir * dither;
        float4 raycastPosVS = mul(unity_CameraProjection, float4(raycastPosWS, 1));
        raycastPosVS.xy /= raycastPosVS.w;
        reflectUV = raycastPosVS.xy * 0.5 + 0.5;
        float4 reflectDepthNormals = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, reflectUV);
        float depth = DecodeFloatRG(reflectDepthNormals.zw) * _ProjectionParams.z + 0.2;
        float reflectDepth = -raycastPosWS.z;
        
        if (length(raycastPosWS - posVS) > _MaxDistance || reflectUV.x < 0.0 || reflectUV.y < 0.0 || reflectUV.x > 1.0 || reflectUV.y > 1.0)
            break;
        
        half depthSign = sign(depth - reflectDepth);
        _StepSize *= depthSign;
        if (abs(depth - reflectDepth) < _Thickness)
            return color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, reflectUV);
    }
    
    return color;
}

#endif