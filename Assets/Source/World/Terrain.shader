Shader "Utopia/Terrain"
{
	Properties
	{
	}
	SubShader
	{
		Pass
		{
			HLSLPROGRAM

			#pragma vertex VS
			#pragma fragment FS

			#include "UnityCG.cginc"

			// Biome Texture Array Property
			UNITY_DECLARE_TEX2DARRAY(_BiomeTextures);

			// Vertex shader inputs
			struct Vertex
			{
				// Object-Space position
				float3 position_OS : POSITION;

				float2 uv : TEXCOORD0;

				// The current vertex's packed biome map sample input
				float4 biome : TEXCOORD1;
			};

			// Fragment shader inputs
			struct Fragment
			{
				// Clip-Space position
				float4 position_Clip : SV_POSITION;

				float2 uv : TEXCOORD0;

				// The split biome map values
				float4 biomeIndex : TEXCOORD1;
				float4 biomeWeighting : TEXCOORD2;
			};

			// Vertex Shader
			void VS(Vertex vertex, out Fragment fragment)
			{
				// Transform position to Clip Space
				fragment.position_Clip = UnityObjectToClipPos(vertex.position_OS);

				// Set UV
				fragment.uv = vertex.uv;

				// Split the biome map sample into biome index and weighting
				fragment.biomeIndex = trunc(vertex.biome);
				fragment.biomeWeighting = frac(vertex.biome);
			}

			// Fragment Shader
			float4 FS(Fragment fragment) : SV_Target
			{
				float4 indices = fragment.biomeIndex;
				float4 weights = fragment.biomeWeighting;

				// Normalise the weights between 0 and 1
				float weightsTotal = weights.x + weights.y + weights.z + weights.w;
				weights /= weightsTotal;

				// Sample the texture maps
				float4 sampleX = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, float3(fragment.uv, indices.x));
				float4 sampleY = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, float3(fragment.uv, indices.y));
				float4 sampleZ = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, float3(fragment.uv, indices.z));
				float4 sampleW = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, float3(fragment.uv, indices.w));

				// Multiply the samples by the weights
				sampleX *= weights.x;
				sampleY *= weights.y;
				sampleZ *= weights.z;
				sampleW *= weights.w;

				// Add the weighted samples
				float4 result = sampleX + sampleY + sampleZ + sampleW;
				return result;
			}

			ENDHLSL
		}
	}
}