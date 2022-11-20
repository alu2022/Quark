#ifndef _QUARK_ScreenSpaceReflection_
#define _QUARK_ScreenSpaceReflection_

#include "Common.hlsl"

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

struct mix_a2v
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;

};
struct mix_v2f
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

CBUFFER_START(UnityPerMaterial)       
    float _MaxStep;
    float _StepSize;
    float _MaxDistance;
    float _Thickness;
CBUFFER_END

TEXTURE2D(_CameraDepthNormalsTexture); SAMPLER(sampler_CameraDepthNormalsTexture);
TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_DitherMap); SAMPLER(sampler_DitherMap);
TEXTURE2D(_ReflectTex); SAMPLER(sampler_ReflectTex);
TEXTURE2D(_SourTex); SAMPLER(sampler_SourTex);

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
    float4 viewRayNDC = float4(i.uv * 2 - 1, 1, 1);
    o.rayVS = mul(unity_CameraInvProjection, viewRayNDC);
    o.rayVS.xyz /= o.rayVS.w;
    return o;
}

bool checkDepthCollision(float3 viewPos, out float2 screenPos, inout float depthDistance)
{
    float4 clipPos = mul(unity_CameraProjection, float4(viewPos, 1.0));
    clipPos = clipPos / clipPos.w;
    screenPos = float2(clipPos.x, clipPos.y) * 0.5 + 0.5;
    
    float4 reflectDepthNormals = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, screenPos);
    float4 depthcolor = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenPos);
    float depth = LinearEyeDepth(depthcolor, _ZBufferParams) + 0.2;
                
    return screenPos.x > 0 && screenPos.y > 0 && screenPos.x < 1.0 && screenPos.y < 1.0 && (depth < -viewPos.z) && depth + _Thickness > -viewPos.z;
}

bool viewSpaceRayMarching(float3 rayOri, float3 rayDir, float currentRayMarchingStepSize, inout float depthDistance, inout float3 currentViewPos, inout float2 hitScreenPos, float2 ditherUV)
{
    float2 offsetUV = fmod(floor(ditherUV), 4.0);
    float ditherValue = SAMPLE_TEXTURE2D(_DitherMap, sampler_DitherMap, offsetUV * 0.25).a;
    rayOri += ditherValue * rayDir;

    UNITY_LOOP
    for (int i = 0; i < _MaxStep; i++)
    {
        float3 currentPos = rayOri + rayDir * currentRayMarchingStepSize * i;

        if (length(rayOri - currentPos) > _MaxDistance)
            return false;
        if (checkDepthCollision(currentPos, hitScreenPos, depthDistance))
        {
            currentViewPos = currentPos;
            return true;
        }
    }
    return false;
}

bool binarySearchRayMarching(float3 rayOri, float3 rayDir, inout float2 hitScreenPos, float2 ditherUV)
{
    float currentStepSize = _StepSize;
    float3 currentPos = rayOri;
    float depthDistance = 0;
   
    UNITY_LOOP
    for (int i = 0; i < _MaxStep; i++)
    {
        if (viewSpaceRayMarching(rayOri, rayDir, currentStepSize, depthDistance, currentPos, hitScreenPos, ditherUV))
        {
            if (depthDistance < _Thickness)
            {
                return true;
            }
            rayOri = currentPos - rayDir * currentStepSize;
            currentStepSize *= 0.5;
        }
        else
        {
            return false;
        }
    }
                 
    return false;
                                 

}

half4 ScreenSpaceReflectionFrag(ssr_v2f i) : SV_Target
{
    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float2 reflectUV = 0;
    
    float4 depthNormals = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, i.uv);
    float linerDepth01 = 0;
    float3 normal = float3(0, 0, 0);
    DecodeDepthNormal(depthNormals, linerDepth01, normal);
    float4 depthcolor = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);
    float linear01Depth = Linear01Depth(depthcolor, _ZBufferParams);
    float3 posVS = i.rayVS.xyz * linear01Depth;
    
    float3 viewDir = normalize(posVS);
    float3 rayDir = normalize(reflect(viewDir, normal));
    
    float2 hitScreenPos = float2(0, 0);
    if (binarySearchRayMarching(posVS, rayDir, hitScreenPos, i.uv))
    {
        float4 reflectTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, hitScreenPos);
        color.rgb = reflectTex.rgb;
    }
    
    return color;
}

mix_v2f MixVert(mix_a2v v)
{
    mix_v2f o;

    o.positionHCS = TransformObjectToHClip(v.positionOS);
    o.uv = v.uv;
    return o;

}
            
half4 MixFrag(mix_v2f i) : SV_Target
{
    half4 reflectTex = SAMPLE_TEXTURE2D(_ReflectTex, sampler_ReflectTex, i.uv);
    half4 sourTex = SAMPLE_TEXTURE2D(_SourTex, sampler_SourTex, i.uv);

    return lerp(sourTex, reflectTex, reflectTex);
}

#endif