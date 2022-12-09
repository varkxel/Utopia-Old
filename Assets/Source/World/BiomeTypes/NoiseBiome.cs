using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using Utopia.Noise;

namespace Utopia.World.BiomeTypes
{
	[CreateAssetMenu(menuName = AssetPath + "Noise Biome", fileName = "Noise Spawn Rule")]
	public class NoiseBiome : Biome
	{
		[Header("Noise Map")]
		public NoiseMap2D noise;

		[Header("Threshold")]
		[Range(0.0f, 1.0f)] public double threshold = 0.5f;
		public NoiseBiomeOperation operation;

		private NativeArray<double> resultsCopy;

		public override JobHandle CalculateWeighting(in int2 chunk, int chunkSize, NativeSlice<double> result)
		{
			int arrayLength = chunkSize * chunkSize;

			noise.CreateJob(chunk, chunkSize, out SimplexFractal2D noiseJob);

			resultsCopy = new NativeArray<double>
			(
				result.Length, Allocator.TempJob,
				NativeArrayOptions.UninitializedMemory
			);
			result.CopyTo(resultsCopy);

			noiseJob.result = resultsCopy;

			JobHandle noiseHandle = noiseJob.Schedule(arrayLength, 4);
			WriteJob writeJob = new WriteJob()
			{
				noise = resultsCopy,
				result = result,

				threshold = this.threshold,
				operation = this.operation.GetOperation()
			};
			return writeJob.Schedule(arrayLength, math.min(chunkSize, 32), noiseHandle);
		}

		public override void OnCompleted()
		{
			base.OnCompleted();
			resultsCopy.Dispose();
		}

		[BurstCompile]
		private struct WriteJob : IJobParallelFor
		{
			public double threshold;
			public FunctionPointer<NoiseBiomeOperations.Operation> operation;

			[ReadOnly]  public NativeArray<double> noise;
			[WriteOnly] public NativeSlice<double> result;

			public void Execute(int index)
			{
				result[index] = operation.Invoke(noise[index], threshold);
			}
		}
	}
}