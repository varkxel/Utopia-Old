using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

using Utopia.Noise;
using Utopia.World.Masks;

namespace Utopia.World
{
	[BurstCompile]
	public class Generator : MonoBehaviour
	{
		public const string AssetPath = "Utopia/Generator/";
		
		[Range(1, uint.MaxValue)] public uint seed = 1;
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
		public int maskSize;
		public NativeArray<float> maskData;
		
		// Heightmap
		public SimplexFractal2D heightmap;
		
		[Header("Heightmap")]
		public SimplexFractal2D.Settings heightmapSettings = SimplexFractal2D.Settings.Default();
		public double heightmapPositionRange = 100000.0;
		
		void Awake()
		{
			// Initialise random
			random = new Random(seed);
			
			// Set the octave positions
			heightmap.Initialise(ref random);
		}
		
		void OnDestroy()
		{
			heightmap.octaveOffsets.Dispose();
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