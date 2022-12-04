#ifndef TOON_COMMON
#define TOON_COMMON

#include "../Common.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float3 color : COLOR;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float4 positionCS : SV_POSITION;
};

sampler2D _MainTex;

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _OutlineColor;
    float _OutlineWidth;
CBUFFER_END

float4 BaseColor(Varyings input) : SV_Target
{
    return tex2D(_MainTex, input.uv);
}

Varyings BaseVertex(Attributes input)
{
    Varyings output;
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    output.positionCS = TransformObjectToHClip(input.positionOS);
    return output;
}


float4 OutlineColor(Varyings input) : SV_Target
{
    return float4(_OutlineColor.rgb, 1);
}

inline float3 UnpackNormalRG(float3 packednormal)
{
    float3 normal;
    normal.xy = packednormal.xy * 2 - 1;
    normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

Varyings OutlineVertex(Attributes input)
{
    Varyings output;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
    VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS);
    float4 scaledScreenParams = GetScaledScreenParams();
    float scaleX = abs(scaledScreenParams.y / scaledScreenParams.x);
    float3 normalCS = TransformWorldToHClipDir(vertexNormalInput.normalWS);
    float4 posCS = vertexInput.positionCS;
    float3 ndcNormal = normalize(normalCS) * posCS.w;
    ndcNormal.x *= scaleX;
    posCS.xy += ndcNormal.xy * _OutlineWidth * saturate(1 / posCS.w);
    output.positionCS = posCS;
    return output;
}

#endif
