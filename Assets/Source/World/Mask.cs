using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

namespace Utopia.World
{
	public class Mask
	{
		public int size;

		private int _complexity;
		public int complexity
		{
			get => _complexity;
			set
			{
				Debug.Assert(complexity % 4 == 0, "complexity divisible by 4");
				_complexity = value;
			}
		}

		public Mask(int size, int complexity = 256)
		{
			this.size = size;
			this.complexity = complexity;
		}

		public void Generate(ref Random random, ref NativeArray<float> output)
		{
			int vertices = random.NextInt(4, 4096);
			AnglesJob anglesJob = new AnglesJob()
			{
				random = random,
				angles = new NativeArray<float>(vertices, Allocator.TempJob)
			};
			JobHandle anglesHandle = anglesJob.Schedule(vertices, 4);
			anglesHandle.Complete();
			random = anglesJob.random;

			NativeArray<float> angles = anglesJob.angles;
			JobHandle anglesSort = angles.SortJob().Schedule();

			

			anglesSort.Complete();
		}

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private struct AnglesJob : IJobParallelFor
		{
			public Random random;

			[WriteOnly] public NativeArray<float> angles;

			public void Execute(int index)
			{
				angles[index] = random.NextFloat(-180.0f + EPSILON, 180.0f);
			}
		}
	}
}
