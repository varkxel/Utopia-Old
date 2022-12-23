using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Utopia.Noise;

namespace Utopia.World.BiomeTypes {
	/// <summary>
	///     A <see cref="Biome" /> generated via a <see cref="NoiseMap2D" />.
	/// </summary>
	[CreateAssetMenu(menuName = AssetPath + "Noise Biome", fileName = "Noise Spawn Rule")]
	public class NoiseBiome : Biome {
		/// <summary>The noise map to use to generate the biome.</summary>
		[Header("Noise Map")] [Tooltip("The noise map to use to generate the biome.")]
		public NoiseMap2D noise;

		/// <summary>The threshold to generate the biome above.</summary>
		[Header("Threshold")] [Tooltip("The threshold to generate the biome above.")] [Range(0.0f, 1.0f)]
		public double threshold = 0.5f;

		/// <summary>The operation to perform on the noise map with the given threshold.</summary>
		[Tooltip("The operation to perform on the noise map with the given threshold.")]
		public NoiseBiomeOperation operation = NoiseBiomeOperation.Greater;

		/// <summary>Biome copy of the results in order to allow for reading whilst other jobs write.</summary>
		private NativeArray<double> resultsCopy;

		public override JobHandle CalculateWeighting(in int2 chunk, int chunkSize, NativeSlice<double> result) {
			if (!noise.octaveOffsetsGenerated)
				// Generate the noise offsets if they haven't already been generated.
				noise.GenerateOffsets(ref Generator.instance.random, true);

			int arrayLength = chunkSize * chunkSize;

			// Create the noise generation job
			noise.CreateJob(chunk, chunkSize, out SimplexFractal2D noiseJob);

			// Clone results to array as a cast from NativeSlice to NativeArray.
			resultsCopy = new NativeArray<double>
			(
				result.Length, Allocator.TempJob,
				NativeArrayOptions.UninitializedMemory
			);
			result.CopyTo(resultsCopy);
			noiseJob.result = resultsCopy;

			// Schedule the writing job
			JobHandle noiseHandle = noiseJob.Schedule(arrayLength, 4);
			WriteJob writeJob = new WriteJob {
				noise = resultsCopy,
				result = result,

				threshold = threshold,
				operation = operation.GetOperation()
			};
			return writeJob.Schedule(arrayLength, math.min(chunkSize, 32), noiseHandle);
		}

		public override void OnComplete() {
			resultsCopy.Dispose();
		}

		/// <summary>
		///     Internal parallel job to write the biome to the biome map array.
		/// </summary>
		[BurstCompile]
		private struct WriteJob : IJobParallelFor {
			// Spawning parameters
			public double threshold;
			public FunctionPointer<NoiseBiomeOperations.Operation> operation;

			// Noise map for the biome
			[ReadOnly] public NativeArray<double> noise;

			// Result output
			[NativeDisableContainerSafetyRestriction] [WriteOnly]
			public NativeSlice<double> result;

			public void Execute(int index) {
				result[index] = operation.Invoke(noise[index], threshold);
			}
		}
	}
}