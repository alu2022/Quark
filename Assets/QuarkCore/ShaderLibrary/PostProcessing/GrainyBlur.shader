Shader "QuarkPostProcessing/GrainyBlur"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_BlurRange("Blur Range",Float) = 0.00015
		_Iteration("Blur Iteration",Float) = 2
	}

	HLSLINCLUDE
		#include "Blur.hlsl"
	ENDHLSL

	SubShader{
		Tags { "RenderPipeline" = "UniversalPipeline" }
		ZTest Always
		ZWrite Off
		Cull Off

		Pass
		{
			Name"GrainyBlur"
			HLSLPROGRAM
				#pragma vertex PostProcessingVert
				#pragma fragment GrainyBlurFrag
			ENDHLSL
		}
	}
}