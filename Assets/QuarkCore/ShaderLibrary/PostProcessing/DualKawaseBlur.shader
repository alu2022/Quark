Shader "QuarkPostProcessing/DualKawaseBlur"
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
			Name"DualKawaseBlurDownSample"
			HLSLPROGRAM
				#pragma vertex DualKawaseDownVert
				#pragma fragment DualKawaseDownFrag
			ENDHLSL
		}

		Pass
		{
			Name"DualKawaseBlurUpSample"
			HLSLPROGRAM
				#pragma vertex DualKawaseUpVert
				#pragma fragment DualKawaseUpFrag
			ENDHLSL
		}
	}
}