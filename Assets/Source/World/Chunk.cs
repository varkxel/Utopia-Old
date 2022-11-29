using UnityEngine;
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
			
		}
	}
}