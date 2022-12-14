using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Utopia.World
{
	public abstract class Biome : ScriptableObject, System.IDisposable
	{
		internal const string AssetPath = BiomeMap.AssetPath + "Types/";

		[SerializeField]
		private AnimationCurve _heightmapModifier = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
		public Curve heightmapModifier;

		public virtual void Initialise()
		{
			heightmapModifier = new Curve(_heightmapModifier, allocator: Allocator.Persistent);
		}

		public virtual void OnComplete() {}

		public virtual void Dispose()
		{
			heightmapModifier.Dispose();
		}

		public abstract JobHandle CalculateWeighting(in int2 chunk, int chunkSize, NativeSlice<double> result);
	}
}