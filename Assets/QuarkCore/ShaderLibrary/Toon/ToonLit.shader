Shader "QuarkToon/ToonLit"
{
	Properties
	{
		_MainTex("_MainTex", 2D) = "white" {}
		_OutlineColor("_OutlineColor", Color) = (0.3,0.3,0.3,1)
		_OutlineWidth("_OutlineWidth", Float) = 0.005
	}

	HLSLINCLUDE
		#include "ToonCommon.hlsl"
	ENDHLSL

	SubShader{

		Pass
		{
			Name "Base Color"
			Tags { "LightMode" = "UniversalForward" }
			Cull off
			HLSLPROGRAM
				#pragma target 3.0
				#pragma vertex BaseVertex
				#pragma fragment BaseColor
			ENDHLSL
		}

		Pass
		{
			Name "Outline"
			Tags { "LightMode" = "SRPDefaultUnlit" }
			Cull front
			HLSLPROGRAM
				#pragma target 3.0
				#pragma vertex OutlineVertex
				#pragma fragment OutlineColor
			ENDHLSL
		}
	}
}