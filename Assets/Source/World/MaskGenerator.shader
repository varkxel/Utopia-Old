Shader "Hidden/Utopia/World/MaskGenerator"
{
	Properties
	{
		_Mainland("Mainland Cutoff", Float) = 0.5
		_Ocean("Ocean Cutoff", Float) = 0.15
	}
	SubShader
	{
		// Disable everything that isn't necessary
		ZWrite Off
		ZTest Off
		Blend Off
		Cull Off
		ZClip False
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex VS
			#pragma fragment FS
			
			#include "UnityCG.cginc"

			// Properties
			CBUFFER_START(UnityPerMaterial)
				float _Mainland;
				float _Ocean;
			CBUFFER_END
			
			struct Fragment
			{
				float4 position_Clip : SV_POSITION;
				float2 position2D : TEXCOORD0;
			};
			
			void VS(float3 position_OS : POSITION, out Fragment output)
			{
				// Transform position
				output.position_Clip = UnityObjectToClipPos(position_OS);

				// Calculate UV from Object-Space position.
				output.position2D = position_OS.xy;
			}
			
			void GS()
			{
			}
			
			float FS(Fragment input) : SV_Target
			{
				// Create a gradient from 0 on the outside to 1 in the centre
				float2 sample2D = 1.0 - abs(input.position2D.xy);
				float sample = min(sample2D.x, sample2D.y);
				
				// Apply the gradient to the result to enforce ocean on the outside, and full height towards the centre
				sample = lerp(0.0 - _Ocean, 1.0 + _Mainland, sample);
				sample = clamp(sample, 0.0, 1.0);
				
				return sample;
			}
			
			ENDHLSL
		}
	}
}
