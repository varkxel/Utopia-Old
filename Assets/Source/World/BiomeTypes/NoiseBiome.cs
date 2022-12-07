using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using Utopia.Noise;
using MathsUtils;

namespace Utopia.World.BiomeTypes
{
	[CreateAssetMenu(menuName = AssetPath + "Noise Biome", fileName = "Noise Spawn Rule")]
	public class NoiseBiome : Biome
	{
		[Header("Noise Map")]
		public NoiseMap2D noise;
		
		[Header("Threshold")]
		[Range(0.0f, 1.0f)] public double threshold = 0.5f;
		public ThresholdOperation thresholdOperation = ThresholdOperation.GreaterEqual;

		private NativeArray<double> noiseMap;

		public override JobHandle Spawn(in int2 chunk, int chunkSize, int layer, NativeArray<int> map, JobHandle? previous)
		{
			int chunkLength = chunkSize * chunkSize;
			
			noise.CreateJob(chunk, chunkSize, out SimplexFractal2D noiseJob);
			
			noiseMap = new NativeArray<double>(chunkLength, Allocator.TempJob);
			noiseJob.result = noiseMap;
			JobHandle noiseJobHandle = noiseJob.Schedule(chunkLength, 4);
			
			WriteJob writeJob = new WriteJob()
			{
				layer = layer,
				chunk = chunk,
				chunkSize = chunkSize,
				
				threshold = this.threshold,
				thresholdOperation = thresholdOperation.GetOperation(),
				
				map = map,
				noise = noiseMap
			};

			JobHandle dependency = noiseJobHandle;
			if(previous != null)
			{
				dependency = JobHandle.CombineDependencies(dependency, previous.Value);
			}
			JobHandle handle = writeJob.Schedule(chunkLength, math.min(64, chunkSize), dependency);
			return handle;
		}

		public override void OnCompleted()
		{
			noiseMap.Dispose();
		}
		
		[BurstCompile]
		private struct WriteJob : IJobParallelFor
		{
			// Current layer
			public int layer;
			
			// Chunk position info
			public int2 chunk;
			public int chunkSize;
			
			// Noise map
			public double threshold;
			public FunctionPointer<ThresholdOperations.Delegate> thresholdOperation;
			[ReadOnly] public NativeArray<double> noise;
			
			// Output
			[WriteOnly] public NativeArray<int> map;
			
			public void Execute(int i)
			{
				int2 position = chunk * chunkSize;
				position += new int2(i % chunkSize, i / chunkSize);
				
				int index = position.x + position.y * chunkSize;
				
				double noiseVal = noise[index];
				if(thresholdOperation.Invoke(noiseVal, threshold))
				{
					// Set map to current layer if selected operation is true
					map[index] = layer;
				}
			}
		}
	}
}