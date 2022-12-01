using UnityEngine;
using Unity.Burst;
using Unity.Collections;
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
		[Range(16, 256)]
		public int chunkSize = 256;
		
		// Mask
		[Header("Mask")]
		public Mask mask;
		public NativeArray<float> maskData;
		
		// Heightmap
		[Header("Heightmap")]
		public NoiseMap2D heightmap;
		
		private void OnValidate()
		{
			if(seed < 1u) seed = 1u;
		}
		
		private void Awake()
		{
			AwakeSingleton();
			
			// Initialise random
			random.InitState(seed);
		}
		
		public void GenerateMask()
		{
			// Generate data for shader & dispatch shader
			mask.Generate(ref random, worldSize);
			
			// Read back shader data asynchronously
			maskData = new NativeArray<float>(worldSize * worldSize, Allocator.Persistent);
			mask.GetResult(ref maskData);
		}
	}
}