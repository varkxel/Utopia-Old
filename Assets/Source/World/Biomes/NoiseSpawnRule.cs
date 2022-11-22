using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Utopia.World.Biomes
{
	[CreateAssetMenu(menuName = AssetPath + "Noise Rule", fileName = "Noise Spawn Rule", order = 1)]
	public class NoiseSpawnRule : SpawnRule
	{
		[Range(0.0f, 1.0f)] public double threshold = 0.5f;
		
		public override void Spawn(in int2 chunk, int chunkSize, int layer, ref NativeArray<int> map)
		{
			
		}
	}
}