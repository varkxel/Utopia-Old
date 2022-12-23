using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Utopia.World.BiomeTypes;
using Random = Unity.Mathematics.Random;

using Utopia.World.Masks;

namespace Utopia.World
{
	[BurstCompile]
	public class Generator : MonoBehaviour
	{
		/// <summary>
		/// Asset menu path for this object and subobjects.
		/// </summary>
		internal const string AssetPath = "Utopia/Generator/";
		
		#region Singleton
		
		public static Generator instance = null;
		
		private void AwakeSingleton()
		{
			if(instance != null)
			{
				Debug.LogError
				(
					$"Multiple {nameof(Generator)} instances exist in the scene!\n" +
					$"Instance \"{instance.name}\" already exists, destroying instance \"{name}\".",
					gameObject
				);
				Destroy(gameObject);
				return;
			}
			instance = this;
		}
		
		#endregion

		[Header("Random Instance")]
		public uint seed = 1;
		[System.NonSerialized] public Random random;

		// World
		[Header("World")]
		public int worldSize = 4096;
		[Range(16, 128)]
		public int chunkSize = 128;

		[SerializeField] internal Material chunkMaterial;

		// Mask
		[Header("Mask")]
		public Mask mask;
		public NativeArray<float> maskData;

		// Heightmap
		[Header("Heightmap")]
		public NoiseMap2D heightmap;

		// Biomes
		[Header("Biomes")]
		public BiomeMap biomes;

		private void OnValidate()
		{
			if(seed < 1u) seed = 1u;
		}

		private void Awake()
		{
			AwakeSingleton();

			// Initialise random
			random.InitState(seed);

			// Initialise biomes
			biomes.Initialise();
		}

		private void OnDestroy()
		{
			foreach(Biome biome in biomes.biomes)
			{
				if(biome is NoiseBiome noiseBiome)
				{
					noiseBiome.noise.DestroyOffsets();
				}
			}
			biomes.Dispose();

			heightmap.DestroyOffsets();
			maskData.Dispose();
		}

		private void Start()
		{
			Generate();
		}
		
		public void Generate()
		{
			GenerateMask();

			// Generate everything, for now.
			for(int x = 0; x < worldSize / chunkSize; x++)
			for(int y = 0; y < worldSize / chunkSize; y++)
			{
				GenerateChunk(new int2(x, y));
			}
		}

		public void GenerateMask()
		{
			// Generate data for shader & dispatch shader
			mask.Generate(ref random, worldSize);
			
			// Read back shader data asynchronously
			maskData = new NativeArray<float>(worldSize * worldSize, Allocator.Persistent);
			mask.GetResult(ref maskData);
		}

		public Chunk GenerateChunk(int2 position)
		{
			GameObject obj = new GameObject();
			obj.transform.SetParent(transform);
			
			float3 position3D = new float3(position.x, 0.0f, position.y);
			position3D *= chunkSize;
			obj.transform.localPosition = position3D;
			
			obj.name = $"Chunk ({position.x.ToString()}, {position.y.ToString()})";

			Chunk chunk = Chunk.Create(obj, position);
			chunk.Generate();
			return chunk;
		}
	}
}