Shader "QuarkPostProcessing/Bloom"
{
	Properties
	{
		_MainTex("_MainTex", 2D) = "white" {}
	}

	HLSLINCLUDE
		#include "Bloom.hlsl"
	ENDHLSL

	SubShader{
		Tags { "RenderPipeline" = "UniversalPipeline" }
		ZTest Always
		ZWrite Off
		Cull Off

		Pass
		{
			Name"BloomBrightness"
			HLSLPROGRAM
				#pragma vertex PostProcessingVert
				#pragma fragment BrightnessFrag
			ENDHLSL
		}

		Pass
		{
			Name"BloomBlurHor"
			HLSLPROGRAM
				#pragma vertex PostProcessingVert
				#pragma fragment DualKawaseDownFrag
			ENDHLSL
		}

		Pass
		{
			Name"BloomBlurVert"
			HLSLPROGRAM
				#pragma vertex PostProcessingVert
				#pragma fragment DualKawaseUpFrag
			ENDHLSL
		}

		Pass
		{
			Name"BloomMix"
			HLSLPROGRAM
				#pragma vertex PostProcessingVert
				#pragma fragment MixFrag
			ENDHLSL
		}
	}
}