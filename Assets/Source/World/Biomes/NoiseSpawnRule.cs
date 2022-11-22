using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Utopia.Noise;
using ThresholdDelegate = Utopia.World.Biomes.ThresholdOperationExtensions.ThresholdDelegate;

namespace Utopia.World.Biomes
{
	[CreateAssetMenu(menuName = AssetPath + "Noise Rule", fileName = "Noise Spawn Rule", order = 1), BurstCompile]
	public class NoiseSpawnRule : SpawnRule
	{
		[Header("Noise Map")]
		public NoiseMap2D noise;
		
		[Header("Threshold")]
		[Range(0.0f, 1.0f)] public double threshold = 0.5f;
		
		public ThresholdOperation thresholdOperation = ThresholdOperation.GreaterEqual;
		
		public override void Spawn(in int2 chunk, int chunkSize, int layer, ref NativeArray<int> map) {
			SimplexFractal2D noiseJob = new SimplexFractal2D()
			{
				
			};
			
			WriteJob writeJob = new WriteJob()
			{
				layer = layer,
				chunk = chunk,
				chunkSize = chunkSize,
				
				threshold = this.threshold,
				thresholdOperation = thresholdOperation.GetOperation(),
				
				
			};
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
			public FunctionPointer<ThresholdDelegate> thresholdOperation;
			[ReadOnly] public NativeArray<float> noise;
			
			// Output
			[WriteOnly] public NativeArray<int> map;
			
			public void Execute(int i)
			{
				int2 position = chunk * chunkSize;
				position += new int2(i % chunkSize, i / chunkSize);
				
				int index = position.x + position.y * chunkSize;
				
				float noiseVal = noise[index];
				if(thresholdOperation.Invoke(noiseVal, threshold)) map[index] = layer;
			}
		}
	}
}