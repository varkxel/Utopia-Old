using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

using Utopia.World.Masks;

namespace Utopia.World
{
	[BurstCompile]
	public class Generator : MonoBehaviour
	{
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
		public int chunkSize = 256;
		
		// Mask
		[Header("Mask")]
		public Mask mask;
		public int maskDivisor = 4;
		
		public bool isMaskGenerated { get; private set; } = false;
		[System.NonSerialized] public int maskSize;
		public NativeArray<float> maskData;
		
		// Heightmap
		[Header("Heightmap")]
		public NoiseMap2D heightmap;
		
		private void OnValidate()
		{
			if(seed <= 1) seed = 1;
			maskDivisor = math.ceilpow2(maskDivisor);
		}
		
		private void Awake()
		{
			AwakeSingleton();
			
			// Initialise random
			random.InitState(seed);
		}
		
		private void OnDestroy()
		{
			DestroyMask();
		}
		
		public void GenerateMask()
		{
			DestroyMask();
			
			maskSize = worldSize / maskDivisor;
			
			// Generate data for shader & dispatch shader
			mask.Generate(ref random, maskSize);
			
			// Read back shader data asynchronously
			maskData = new NativeArray<float>(maskSize * maskSize, Allocator.Persistent);
			mask.GetResult(ref maskData);
		}
		
		public void DestroyMask()
		{
			if(isMaskGenerated)
			{
				maskData.Dispose();
				maskSize = 0;
				isMaskGenerated = false;
			}
		}
		
		public void GenerateChunk(in int2 position)
		{
			GameObject chunkObject = new GameObject();
			chunkObject.transform.SetParent(transform);
			chunkObject.name = $"Chunk ({position.x.ToString()}, {position.y.ToString()})";
			
			Chunk chunk = chunkObject.AddComponent<Chunk>();
			chunk.generator = this;
			chunk.index = position;
			chunk.size = chunkSize;
			
			chunk.Generate();
		}
	}
}