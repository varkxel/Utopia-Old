using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Utopia.World.Biomes
{
	public abstract class Biome : ScriptableObject
	{
		internal const string AssetPath = Generator.AssetPath + "Biomes/Spawn Rules/";
		
		public abstract void Spawn(ref Random random, in int2 chunk, int chunkSize, int layer, ref NativeArray<int> map);
	}
}