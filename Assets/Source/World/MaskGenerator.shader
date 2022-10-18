Shader "Hidden/Utopia/World/MaskGenerator"
{
	Properties
	{
		_Seed("Seed", Float) = 0
		
		_Octaves("Octaves", Integer) = 4
		_Lacunarity("Lacunarity", Float) = 2.0
		_Gain("Gain", Float) = 0.5
	}
	SubShader
	{
		// Disable everything
		ZWrite On
		ZTest On
		Blend Off
		Cull Off
		ZClip True
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vertex
			#pragma fragment Fragment
			
			#include "Assets/Source/Noise/Noise.hlsl"
			
			StructuredBuffer<float> angles;
			
			uniform float _Seed;
			uniform uint _Octaves;
			uniform float _Lacunarity;
			uniform float _Gain;
			
			struct VertexInfo
			{
				uint vertexID : SV_VertexID;
			};
			
			struct FragmentInfo
			{
				float4 position_HCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			FragmentInfo Vertex(VertexInfo input)
			{
				FragmentInfo output;
				
				
				
				return output;
			}
			
			void Geometry()
			{
			}
			
			float4 Fragment(FragmentInfo input) : SV_Target
			{
				return 1;
			}
			
			ENDHLSL
		}
	}
}
