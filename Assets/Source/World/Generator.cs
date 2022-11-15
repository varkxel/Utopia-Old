using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Events;
using static Unity.Mathematics.math;
using UnityEngine.Rendering;
using Utopia.Noise;
using float2 = Unity.Mathematics.float2;
using Random = Unity.Mathematics.Random;

namespace Utopia.World
{
	[BurstCompile]
	public class Generator : MonoBehaviour
	{
		[Range(1, uint.MaxValue)] public uint seed = 1;
		[System.NonSerialized] public Random random;

		// World
		[Header("World")]
		public int worldSize = 4096;

		// Mask
		[Header("Mask")]
		public Mask mask = new Mask();
		public int maskDivisor = 4;

		public bool isMaskGenerated { get; private set; } = false;
		private int maskSize;
		public NativeArray<float> maskData;

		// Heightmap
		[Header("Heightmap")]
		public SimplexFractal2D heightmap = new SimplexFractal2D()
		{
			scale = 1.0,
			octaves = 5,
			gain = 0.5,
			lacunarity = 2.0
		};
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
				
				float baseSample = mask[baseIndex.x + baseIndex.y * chunkMaskSize];
				float offsetSample = mask[offsetIndex.x + offsetIndex.y * chunkMaskSize];
				float value = lerp(baseSample, offsetSample, offsetMagnitude);
				chunkMask[index] = value;
			}
		}

		internal SampleMaskJob CreateSampleMaskJob(in NativeArray<float> chunkMaskArray, int2 chunk, int chunkSize)
		{
			return new SampleMaskJob()
			{
				mask = maskData,
				chunkMask = chunkMaskArray,
				maskDivisor = maskDivisor,

				chunk = chunk,
				chunkSize = chunkSize
			};
		}

		void Awake()
		{
			// Initialise random
			random = new Random(seed);
			
			// Randomize the heightmap's origin
			heightmap.origin = random.NextDouble2(-heightmapPositionRange, heightmapPositionRange);
			
			// Set the octave offsets
			heightmap.octaveOffsets = new NativeArray<double2>(heightmap.octaves, Allocator.Persistent);
			for(int octave = 0; octave < heightmap.octaves; octave++)
			{
				heightmap.octaveOffsets[octave] = random.NextDouble2(-heightmapPositionRange, heightmapPositionRange);
			}
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
			mask.size = maskSize;

			mask.Generate(ref random);
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
			chunk.Generate();
		}
	}
}