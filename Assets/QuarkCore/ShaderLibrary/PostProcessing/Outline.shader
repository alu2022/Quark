Shader "QuarkPostProcessing/Outline"
{
	Properties
	{
		_MainTex("_MainTex", 2D) = "white" {}
		_OutlineThickness("_OutlineThickness", Float) = 0
		_DepthSensitivity("_DepthSensitivity", Float) = 0
		_NormalsSensitivity("_NormalsSensitivity", Float) = 0
		_ColorSensitivity("_ColorSensitivity", Float) = 0
		_OutlineColor("_OutlineColor", Color) = (1,1,1,1)
	}

	HLSLINCLUDE
		#include "Outline.hlsl"
	ENDHLSL

	SubShader{
		Tags { "RenderPipeline" = "UniversalPipeline" }
		ZTest Always
		ZWrite Off
		Cull Off

		Pass
		{
			Name"Outline"
			HLSLPROGRAM
				#pragma vertex OutlineVert
				#pragma fragment OutlineFrag
			ENDHLSL
		}
	}
}