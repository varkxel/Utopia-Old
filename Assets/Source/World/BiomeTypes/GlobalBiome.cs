using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Utopia.World.BiomeTypes
{
	/// <summary>
	/// A spawn rule that completely covers the map, independent of what is there.
	/// Use this for oceans.
	/// </summary>
	[CreateAssetMenu(menuName = AssetPath + "Global Biome")]
	public class GlobalBiome : Biome
	{
		public float threshold = 0.2f;

		public override JobHandle CalculateWeighting(in int2 chunk, int chunkSize, NativeSlice<double> result)
		{
			WriteJob writeJob = new WriteJob()
			{
				threshold = this.threshold,
				map = result
			};
			return writeJob.Schedule(chunkSize * chunkSize, math.min(chunkSize, 128));
		}

		[BurstCompile]
		private struct WriteJob : IJobParallelFor
		{
			// Input
			public float threshold;

			// Output
			[NativeDisableContainerSafetyRestriction]
			[WriteOnly] public NativeSlice<double> map;

			public void Execute(int index)
			{
				map[index] = threshold;
			}
		}
	}
}