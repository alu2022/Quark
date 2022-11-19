Shader "QuarkPostProcessing/ScreenSpaceReflection"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
        _MaxStep("MaxStep",Float) = 10
        _StepSize("StepSize",Float) = 1
        _MaxDistance("MaxDistance",Float) = 10
        _Thickness("Thickness",Float) = 1
		_BlurRange("Blur Range",Float) = 0.00015
	}

	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Tags { "LightMode" = "UniversalForward" }
			Name"ScreenSpaceReflection"

			HLSLINCLUDE
				#include "ScreenSpaceReflection.hlsl"
			ENDHLSL

			HLSLPROGRAM
				#pragma vertex ScreenSpaceReflectionVert
				#pragma fragment ScreenSpaceReflectionFrag
			ENDHLSL
		}

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
			Name"DualKawaseBlurDownSample"

			HLSLINCLUDE
				#include "Blur.hlsl"
			ENDHLSL

			HLSLPROGRAM
				#pragma vertex PostProcessingVert
				#pragma fragment DualKawaseDownFrag
			ENDHLSL
        }
        
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
			Name"DualKawaseBlurUpSample"

			HLSLINCLUDE
				#include "Blur.hlsl"
			ENDHLSL

			HLSLPROGRAM
				#pragma vertex PostProcessingVert
				#pragma fragment DualKawaseUpFrag
			ENDHLSL
        }
	}
}