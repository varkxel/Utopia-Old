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
				float4 biomeIndex : TEXCOORD1;
				float4 biomeWeighting : TEXCOORD2;
			};

			void VS(Vertex vertex, out Fragment fragment)
			{
				fragment.position_Clip = UnityObjectToClipPos(vertex.position_OS);
				fragment.uv = vertex.uv;
				fragment.biomeIndex = trunc(vertex.biome);
				fragment.biomeWeighting = frac(vertex.biome);
			}

			float4 FS(Fragment fragment) : SV_Target
			{
				float4 biomeIndices = fragment.biomeIndex;
				float4 weights = fragment.biomeWeighting;
				float weightsTotal = weights.x + weights.y + weights.z + weights.w;
				weights /= weightsTotal;

				float4 sampleX = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, float3(fragment.uv, biomeIndices.x));
				float4 sampleY = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, float3(fragment.uv, biomeIndices.y));
				float4 sampleZ = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, float3(fragment.uv, biomeIndices.z));
				float4 sampleW = UNITY_SAMPLE_TEX2DARRAY(_BiomeTextures, float3(fragment.uv, biomeIndices.w));

				sampleX *= weights.x;
				sampleY *= weights.y;
				sampleZ *= weights.z;
				sampleW *= weights.w;

				float4 result = sampleX + sampleY + sampleZ + sampleW;
				return result;
			}

			ENDHLSL
		}
	}
}