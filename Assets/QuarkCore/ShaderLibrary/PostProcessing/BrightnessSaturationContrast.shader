Shader "QuarkPostProcessing/BrightnessSaturationContrast"
{
	Properties
	{
		_MainTex("_MainTex", 2D) = "white" {}
		_briSatConThickness("_Brightness", Float) = 0
		_DepthSensitivity("_Saturation", Float) = 0
		_NormalsSensitivity("_Contrast", Float) = 0
	}

	HLSLINCLUDE
		#include "BrightnessSaturationContrast.hlsl"
	ENDHLSL

	SubShader{
		Tags { "RenderPipeline" = "UniversalPipeline" }
		ZTest Always
		ZWrite Off
		Cull Off

		Pass
		{
			Name"BriSatCon"
			HLSLPROGRAM
				#pragma vertex BriSatConVert
				#pragma fragment BriSatConFrag
			ENDHLSL
		}
	}
}