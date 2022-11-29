using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace Utopia.World.Biomes
{
	public abstract class Biome : ScriptableObject
	{
		internal const string AssetPath = BiomeMap.AssetPath + "Types/";
		
		public abstract void Spawn(in int2 chunk, int chunkSize, int layer, ref NativeArray<int> map);
	}
}