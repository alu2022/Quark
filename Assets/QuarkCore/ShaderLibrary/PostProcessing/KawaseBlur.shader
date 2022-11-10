Shader "QuarkPostProcessing/KawaseBlur"
{
	Properties
	{
		_MainTex("_MainTex", 2D) = "white" {}
		_BlurRange("Blur Range",Float) = 0.00015
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
			Name"KawaseBlur"
			HLSLPROGRAM
				#pragma vertex PostProcessingVert
				#pragma fragment KawaseBlurFrag
			ENDHLSL
		}
	}
}