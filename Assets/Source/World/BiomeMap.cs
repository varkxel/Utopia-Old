using System;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using System.Collections.Generic;
using Unity.Jobs;

namespace Utopia.World
{
	[CreateAssetMenu(menuName = AssetPath + "Biome Map", fileName = "Biome Map")]
	public class BiomeMap : ScriptableObject
	{
		internal const string AssetPath = Generator.AssetPath + "Biomes/";

		[Header("Biome List")]
		public List<Biome> biomes = new List<Biome>();

		/// <summary>
		/// Generates a map using the stored biome spawn rule list.
		/// </summary>
		/// <param name="random">The random instance to utilise.</param>
		/// <param name="chunk">The chunk index to generate.</param>
		/// <param name="chunkSize">The size of the chunk to generate.</param>
		/// <param name="map">
		/// The map to generate the biome map into.
		/// Needs to be chunkSize * chunkSize length.
		/// </param>
		public JobHandle GenerateChunk(in int2 chunk, int chunkSize, ref NativeArray<int> map)
		{
			int biomeCount = biomes.Count;
			JobHandle? previous = null;
			for(int i = 0; i < biomeCount; i++)
			{
				#if UNITY_EDITOR || DEVELOPMENT_BUILD
				// Biome null check for editor/development only, as it's an expensive operation.
				if(biomes[i] == null)
				{
					// Throw warning message if not in-editor.
					throw new NullReferenceException($"Tried to generate chunk from {nameof(BiomeMap)} \"{name}\" with a null biome set at index {i.ToString()}.");
				}
				#endif

				previous = biomes[i].Spawn(chunk, chunkSize, i, map, previous);
			}
			return previous ?? default;
		}

		public void OnCompleted()
		{
			// Free biome data
			for(int i = 0; i < biomes.Count; i++)
			{
				biomes[i].OnCompleted();
			}
		}
	}
}