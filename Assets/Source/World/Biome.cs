using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Utopia.World {
	/// <summary>
	///     A biome that can be spawned in the world.
	///     Contains information on how a "climate" or region in the world should be spawned and behave.
	/// </summary>
	public abstract class Biome : ScriptableObject, IDisposable {
		/// <summary>
		///     Asset menu path for this object and subobjects.
		/// </summary>
		internal const string AssetPath = BiomeMap.AssetPath + "Types/";

		/// <summary>
		///     The curve to use as a heightmap modifier.
		/// </summary>
		[SerializeField] private AnimationCurve _heightmapModifier = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

		/// <summary>
		///     The texture to use for/pack into the Terrain shader texture array.
		/// </summary>
		public Texture2D biomeTexture;

		/// <summary>
		///     The DOTS-compatible curve object.
		/// </summary>
		public Curve heightmapModifier;

		/// <summary>
		///     Called when the biome map is destroyed.
		/// </summary>
		public virtual void Dispose() {
			heightmapModifier.Dispose();
		}

		/// <summary>
		///     Called when the biome map is initialised.
		/// </summary>
		public virtual void Initialise() {
			heightmapModifier = new Curve(_heightmapModifier, Allocator.Persistent);
		}

		/// <summary>Calculates the weighting (priority) of the biome.</summary>
		/// <param name="chunk">The chunk index to generate weighting info for.</param>
		/// <param name="chunkSize">The size of the chunk to generate info for.</param>
		/// <param name="result">The array to store the weighting results in.</param>
		/// <returns>A <see cref="JobHandle" /> to the weighting calculation job.</returns>
		public abstract JobHandle CalculateWeighting(in int2 chunk, int chunkSize, NativeSlice<double> result);

		/// <summary>
		///     Called when the biome has completed generating.
		/// </summary>
		public virtual void OnComplete() { }
	}
}