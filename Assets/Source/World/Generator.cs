using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
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
		private int maskSize;
		public NativeArray<float> maskData;

		// Heightmap
		public SimplexFractal2D heightmap;
		
		[Header("Heightmap")]
		public SimplexFractal2D.Settings heightmapSettings = SimplexFractal2D.Settings.Default();
		public double heightmapPositionRange = pow(2, 24);

		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		internal struct SampleMaskJob : IJobParallelFor
		{
			[ReadOnly]  public NativeArray<float> mask;
			[WriteOnly] public NativeArray<float> chunkMask;

			public int2 chunk;
			public int chunkSize;
			public int maskDivisor;

			public void Execute(int index)
			{
				int chunkMaskSize = chunkSize / maskDivisor;

				int2 chunkIndex = chunk * chunkSize;
				int2 indexInChunk = int2(index % chunkSize, index / chunkSize);
				int2 sampleIndex = chunkIndex + indexInChunk;

				float2 maskSample = float2(sampleIndex) / (float) maskDivisor;
				float2 roundedPosition = round(maskSample);

				float2 offset = maskSample - roundedPosition;
				float2 offsetDirection = normalizesafe(offset);
				float offsetMagnitude = length(offset);
				
				int2 baseIndex = (int2) roundedPosition;
				int2 offsetIndex = (int2) round(offsetDirection);
				offsetIndex += baseIndex;
				offsetIndex = clamp(offsetIndex, 0, chunkSize / maskDivisor);
				
				float baseSample = mask[baseIndex.x + baseIndex.y * chunkMaskSize];
				float offsetSample = mask[offsetIndex.x + offsetIndex.y * chunkMaskSize];
				float value = lerp(baseSample, offsetSample, offsetMagnitude);
				chunkMask[index] = value;
			}
		}

		void Awake()
		{
			// Initialise random
			random = new Random(seed);
			
			// Randomize the heightmap's origin
			heightmap.origin = random.NextDouble2(-heightmapPositionRange, heightmapPositionRange);
			
			// Set the octave offsets
			heightmap.Initialise(ref random);
		}
		
		void OnDestroy()
		{
			heightmap.octaveOffsets.Dispose();
			ResetMask();
		}

		public void ResetMask()
		{
			if(isMaskGenerated)
			{
				maskData.Dispose();
				isMaskGenerated = false;
			}
		}

		public void GenerateMask(UnityAction onComplete = null)
		{
			ResetMask();

			maskSize = worldSize / maskDivisor;
			mask.Generate(ref random, maskSize);
			maskData = new NativeArray<float>(maskSize * maskSize, Allocator.Persistent);
			AsyncGPUReadback.RequestIntoNativeArray(ref maskData, mask.gpuResult, 0, request =>
			{
				if(request.hasError)
				{
					Debug.LogError("Error requesting island mask data from GPU.");
					return;
				}

				isMaskGenerated = true;
				mask.gpuResult.DiscardContents();

				onComplete?.Invoke();
			});
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