using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Utopia.Noise;

namespace Utopia.World
{
	public class Chunk : MonoBehaviour
	{
		public bool generated { get; private set; } = false;

		[HideInInspector] public int2 index;
		[HideInInspector] public int size;

		internal static Chunk Create(GameObject obj, int2 index)
		{
			Chunk chunk = obj.AddComponent<Chunk>();
			chunk.index = index;
			chunk.size = Generator.instance.chunkSize;

			chunk.Generate();
			return chunk;
		}

		private NativeArray<double> heightmap;
		private int heightmapSampleSize => size + 1;

		private JobHandle? heightmapJob = null; 

		public void Generate()
		{
			int sampleSize = heightmapSampleSize;
			heightmap = new NativeArray<double>(sampleSize * sampleSize, Allocator.Persistent);
			Generator.instance.heightmap.CreateJob(index, sampleSize, out SimplexFractal2D generator);

			generator.result = heightmap;
			heightmapJob = generator.Schedule(heightmap.Length, 1);
			
			
		}

		private void Update()
		{
			if(heightmapJob == null) return;
			if(!heightmapJob.Value.IsCompleted) return;
			
			
		}
	}
}