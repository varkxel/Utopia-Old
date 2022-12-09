using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Utopia.World
{
	public abstract class Biome : ScriptableObject
	{
		internal const string AssetPath = BiomeMap.AssetPath + "Types/";

		public AnimationCurve heightMultiplier = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

		public abstract JobHandle CalculateWeighting(in int2 chunk, int chunkSize, NativeSlice<double> result);
		public virtual void OnCompleted() {}
	}
}