using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using Utopia.Noise;
using Utopia.World.Masks;

namespace Utopia.World
{
	public class Chunk : MonoBehaviour
	{
		public Generator generator { get; internal set; }
		
		[HideInInspector] public int2 index;
		[HideInInspector] public int size = 256;
		
		internal NativeArray<double> heightmap;
		
		// TODO implement
		public void Generate()
		{
			// Calculate sizeSq
			int sizeSq = size * size;
			
			// Create heightmap job
			generator.heightmap.CreateJob(index, size, out SimplexFractal2D heightmapJob);
			heightmap = new NativeArray<double>(sizeSq, Allocator.TempJob);
			heightmapJob.result = heightmap;
			
			JobHandle heightmapJobHandle = heightmapJob.Schedule(sizeSq, 4);
			
			NativeArray<float> mask = generator.maskData;
			NativeArray<float> chunkMask = new NativeArray<float>(sizeSq, Allocator.TempJob);
			MaskSampler sampleMaskJob = new MaskSampler()
			{
				mask = mask,
				chunkMask = chunkMask,

				chunk = index,
				chunkSize = size,

				maskSize = generator.maskSize,
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