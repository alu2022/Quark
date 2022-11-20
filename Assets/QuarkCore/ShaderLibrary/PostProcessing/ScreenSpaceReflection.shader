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

            Pass{

            //pass3

                          Name"Mask"//Pass3

                           Tags{
                                         "LightMode" = "UniversalForward"}


                         ZTest on
                         ZWrite on
                         Cull back


                                    HLSLPROGRAM

                                    #pragma vertex vert_mask
                        #pragma fragment frag_mask

                               struct a2v {
                                    float4 positionOS:POSITION;
                                    float2 uv           : TEXCOORD0;
                               };
                                struct v2f {
                                    float4 positionHCS : SV_POSITION;
                                    float2 uv           : TEXCOORD0;
                                };


                                v2f vert_mask(a2v v)
                                {
                                    v2f o;

                                   o.positionHCS = TransformObjectToHClip(v.positionOS);
                                   return o;

                            }
                                half4 frag_mask(v2f i) :SV_Target
                                {
                                    return float4(1,1,1,1);

                                }

                        ENDHLSL
                   }

        Pass{
            Name"Mix"
            Tags{"LightMode" = "UniversalForward"}

            ZTest Off
            Cull Off
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex MixVert
            #pragma fragment MixFrag

            ENDHLSL
        }
	}
}