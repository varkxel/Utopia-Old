Shader "Hidden/Utopia/World/MaskGenerator"
{
	Properties
	{
		_Mainland("Mainland Cutoff", Float) = 0.5
		_Ocean("Ocean Cutoff", Float) = 0.15
	}
	SubShader
	{
		// Disable everything
		ZWrite Off
		ZTest Off
		Blend Off
		Cull Off
		ZClip False

		Pass
		{
			HLSLPROGRAM

			#pragma vertex Vertex
			#pragma fragment Fragment

			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float _Mainland;
				float _Ocean;
			CBUFFER_END

			struct FragmentInfo
			{
				float4 position_HCS : SV_POSITION;
				float2 position2D : TEXCOORD0;
			};

			void Vertex(float3 position_OS : POSITION, out FragmentInfo output)
			{
				float3 position_WS = TransformObjectToWorld(position_OS);
				float4 position_HCS = TransformWorldToHClip(position_WS);

				output.position_HCS = position_HCS;
				output.position2D = position_OS.xy;
			}

			void Geometry()
			{
			}

			float Fragment(FragmentInfo input) : SV_Target
			{
				float2 sample2D = 1.0 - abs(input.position2D.xy);
				float sample = min(sample2D.x, sample2D.y);
				
				// Blur the mask to enforce ocean on the outside, and full height towards the centre
				sample = lerp(0.0 - _Ocean, 1.0 + _Mainland, sample);
				sample = clamp(sample, 0.0, 1.0);
				
				return sample;
			}
			
			ENDHLSL
		}
	}
}
