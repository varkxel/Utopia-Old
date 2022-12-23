using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Utopia.World.BiomeTypes {
	/// <summary>
	///     A spawn rule that completely covers the map, independent of what is there.
	///     Use this for oceans.
	/// </summary>
	[CreateAssetMenu(menuName = AssetPath + "Global Biome")]
	public class GlobalBiome : Biome {
		/// <summary>
		///     The base threshold to globally set the biomes to.
		/// </summary>
		public float threshold = 0.01f;

		public override JobHandle CalculateWeighting(in int2 chunk, int chunkSize, NativeSlice<double> result) {
			// Schedule the write job
			WriteJob writeJob = new WriteJob {
				threshold = threshold,
				map = result
			};
			return writeJob.Schedule(chunkSize * chunkSize, math.min(chunkSize, 128));
		}

		/// <summary>
		///     Internal parallel job to write the threshold to the biome map array.
		/// </summary>
		[BurstCompile]
		private struct WriteJob : IJobParallelFor {
			// Input threshold
			public float threshold;

			// Output biome map
			[NativeDisableContainerSafetyRestriction] [WriteOnly]
			public NativeSlice<double> map;

			public void Execute(int index) {
				map[index] = threshold;
			}
		}
	}
}