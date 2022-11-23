using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace Utopia.World.Biomes
{
	public abstract class SpawnRule : ScriptableObject
	{
		internal const string AssetPath = Generator.AssetPath + "Biomes/Spawn Rules/";
		
		public abstract void Spawn(in int2 chunk, int chunkSize, int layer, ref NativeArray<int> map);
	}
}