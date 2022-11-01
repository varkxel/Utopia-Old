Shader "Hidden/Utopia/World/MaskGenerator"
{
	Properties
	{
		_Seed("Seed", Float) = 0
		
		_Scale("Scale", Float) = 360.0
		_Octaves("Octaves", Integer) = 4
		_Lacunarity("Lacunarity", Float) = 2.0
		_Gain("Gain", Float) = 0.5
		
		_Base("Base Mask", Float) = 0.5
		_Cutoff("Cutoff Mask", Float) = 0.15 
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
			#include "Assets/Source/Noise/Noise.hlsl"
			
			StructuredBuffer<float> angles;

			CBUFFER_START(UnityPerMaterial)
				float _Seed;
				
				float _Scale;
				uint _Octaves;
				float _Lacunarity;
				float _Gain;
				
				float _Base;
				float _Cutoff;
			CBUFFER_END
			
			struct VertexInfo
			{
				uint id : SV_VertexID;
				float2 uv : TEXCOORD0;
			};
			
			struct FragmentInfo
			{
				float4 position_HCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			float2 GetDirection(float angle)
			{
				return float2(cos(angle), sin(angle));
			}
			
			FragmentInfo Vertex(VertexInfo input)
			{
				uint id = input.id;
				float angle = angles[id];
				float2 vertex = GetDirection(angle);

				float noisePosition = _Seed;
				noisePosition += angle * _Scale;
				vertex *= NoiseFractal(noisePosition, _Octaves, _Lacunarity, _Gain);
				
				FragmentInfo output;
				output.position_HCS = float4(vertex, 0.0f, 1.0f);
				output.uv = input.uv;
				return output;
			}
			
			void Geometry()
			{
			}
			
			float4 Fragment(FragmentInfo input) : SV_Target
			{
				float2 mask = input.uv;
				mask -= 0.5;
				mask = 0.5 - abs(mask);
				mask *= 2.0;

				mask = lerp(0.0 - _Base, 1.0 + _Cutoff, mask);
				mask = clamp(mask, 0.0, 1.0);

				float maskValue = (mask.x + mask.y) / 2.0;
				return maskValue;
			}
			
			ENDHLSL
		}
	}
}
