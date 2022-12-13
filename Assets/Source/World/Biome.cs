using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Utopia.World
{
	public abstract class Biome : ScriptableObject
	{
		internal const string AssetPath = BiomeMap.AssetPath + "Types/";

		public Curve heightmapModifier = new Curve();

		public abstract JobHandle CalculateWeighting(in int2 chunk, int chunkSize, NativeSlice<double> result);
		public virtual void OnCompleted() {}
	}
}