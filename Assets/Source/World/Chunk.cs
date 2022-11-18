using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using Utopia.Noise;

namespace Utopia.World
{
	public class Chunk : MonoBehaviour
	{
		public Generator generator { get; internal set; }
		
		[HideInInspector] public int2 index;
		[HideInInspector] public int size = 256;
		
		private int sizeSq;
		
		internal NativeArray<double> heightmap;
		
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
			
			NativeArray<float> mask = generator.maskData;
			NativeArray<float> chunkMask = new NativeArray<float>(sizeSq, Allocator.TempJob);
			Generator.SampleMaskJob sampleMaskJob = new Generator.SampleMaskJob()
			{
				mask = mask,
				chunkMask = chunkMask,

				chunk = index,
				chunkSize = size,

				maskDivisor = generator.maskDivisor
			};
			JobHandle sampleMaskJobHandle = sampleMaskJob.Schedule(sizeSq, 8);
			
			CombineMask combineMaskJob = new CombineMask()
			{
				heightmap = this.heightmap,
				mask = chunkMask
			};
			JobHandle combineMaskJobHandle = combineMaskJob.Schedule
			(
				sizeSq, 64,
				JobHandle.CombineDependencies(heightmapJobHandle, sampleMaskJobHandle)
			);
			
			combineMaskJobHandle.Complete();
			
			chunkMask.Dispose();
		}
		
		private void OnDestroy()
		{
			heightmap.Dispose();
		}
		
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private struct CombineMask : IJobParallelFor
		{
			public NativeArray<double> heightmap;
			[ReadOnly] public NativeArray<float> mask;
			
			public void Execute(int index)
			{
				heightmap[index] *= mask[index];
			}
		}
	}
}