using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Utopia.World.Biomes
{
	/// <summary>
	/// A spawn rule that completely covers the map, independent of what is there.
	/// Use this for oceans.
	/// </summary>
	[CreateAssetMenu(menuName = AssetPath + "Global Biome")]
	public class GlobalBiome : Biome
	{
		public override void Spawn(in int2 chunk, int chunkSize, int layer, ref NativeArray<int> map)
		{
			WriteJob writeJob = new WriteJob()
			{
				chunk = chunk,
				chunkSize = chunkSize,
				layer = layer,
				map = map
			};
			writeJob.Schedule(chunkSize * chunkSize, math.min(64, chunkSize)).Complete();
		}
		
		[BurstCompile]
		private struct WriteJob : IJobParallelFor
		{
			// Current layer
			public int layer;
			
			// Chunk position info
			public int2 chunk;
			public int chunkSize;
			
			// Output
			[WriteOnly] public NativeArray<int> map;
			
			public void Execute(int index)
			{
				int2 position = chunk * chunkSize;
				position += new int2(index % chunkSize, index / chunkSize);

				map[position.x + position.y * chunkSize] = layer;
			}
		}
	}
}