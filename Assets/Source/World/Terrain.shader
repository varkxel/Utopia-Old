Shader "Utopia/Terrain"
{
	Properties
	{
		//_BiomeTextures("Biome Textures", 2DArray) = "" {}
		//_BiomeBlend("Biome Blending Threshold", Float) = 0.1
	}
	SubShader
	{
		Pass
		{
			HLSLPROGRAM

			#pragma vertex VS
			#pragma fragment FS

			#include "UnityCG.cginc"
			#include "Assets/Source/Utils.hlsl"

			uniform float _BiomeBlend;

			UNITY_DECLARE_TEX2DARRAY(_BiomeTextures);

			struct Vertex
			{
				float3 position_OS : POSITION;
				float2 uv : TEXCOORD0;
				float4 biome : TEXCOORD1;
			};

			struct Fragment
			{
				float4 position_Clip : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 biome : TEXCOORD1;
			};

			void VS(Vertex vertex, out Fragment fragment)
			{
				fragment.position_Clip = UnityObjectToClipPos(vertex.position_OS);
				fragment.uv = vertex.uv;
				fragment.biome = vertex.biome;
			}

			float4 FS(Fragment fragment) : SV_Target
			{
				float4 biomeIndices = trunc(fragment.biome);
				float4 weights = frac(fragment.biome);
				
				float4 sampleX = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, biomeIndices.x);
				float4 sampleY = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, biomeIndices.y);
				float4 sampleZ = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, biomeIndices.z);
				float4 sampleW = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, biomeIndices.w);

				float maxWeight = cmax(weights);
				float4 difference = maxWeight - weights;

				float4 shouldBlend = difference <= _BiomeBlend;
				weights = unlerp(_BiomeBlend, 0.0f, difference);

				// Exclude out of threshold values
				weights *= shouldBlend;

				// Blend values by weights
				sampleX *= weights.x;
				sampleY *= weights.y;
				sampleZ *= weights.z;
				sampleW *= weights.w;

				sampleX = clamp(sampleX, 0, 1);
				sampleY = clamp(sampleY, 0, 1);
				sampleZ = clamp(sampleZ, 0, 1);
				sampleW = clamp(sampleW, 0, 1);

				// Sum the samples & weights
				float4 sampleTotal = sampleX + sampleY + sampleZ + sampleW;
				float weightsTotal = weights.x + weights.y + weights.z + weights.w;

				// Calculate weighted average
				float4 value = sampleTotal / weightsTotal;
				return value;
			}

			ENDHLSL
		}
	}
}