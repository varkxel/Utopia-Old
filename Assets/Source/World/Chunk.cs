using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Utopia.Noise;

namespace Utopia.World
{
	public class Chunk : MonoBehaviour
	{
		public Generator generator { get; internal set; }

		public int2 index;
		public int size = 256;

		private int sizeSq;
		
		private NativeArray<double> heightmap;
		
		public void Generate()
		{
			// Calculate sizeSq
			sizeSq = size * size;
			
			SimplexFractal2D heightmapJob = generator.heightmap;
			heightmapJob.size = size;
			heightmapJob.index = index;
			
			heightmap = new NativeArray<double>(sizeSq, Allocator.TempJob);
			heightmapJob.result = heightmap;
			
			JobHandle heightmapJobHandle = heightmapJob.Schedule(sizeSq, 4);

			NativeArray<float> mask = new NativeArray<float>(sizeSq, Allocator.TempJob);
			Generator.SampleMaskJob sampleMaskJob = generator.CreateSampleMaskJob(mask, index, size);
			JobHandle sampleMaskJobHandle = sampleMaskJob.Schedule(sizeSq, 8);

			sampleMaskJobHandle.Complete();
			heightmapJobHandle.Complete();
			
			CombineMask combineMaskJob = new CombineMask()
			{
				heightmap = this.heightmap,
				mask = mask
			};
			JobHandle combineMaskJobHandle = combineMaskJob.Schedule
			(
				sizeSq, 64,
				JobHandle.CombineDependencies(heightmapJobHandle, sampleMaskJobHandle)
			);
			
			combineMaskJobHandle.Complete();
			mask.Dispose();
			heightmap.Dispose();
		}

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private struct CombineMask : IJobParallelFor
		{
			public NativeArray<double> heightmap;
			[ReadOnly] public NativeArray<float> mask;

			public void Execute(int index)
			{
				//heightmap[index] *= mask[index];
			}
		}
	}
}