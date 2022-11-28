using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

using System.Collections.Generic;

namespace Utopia.World.Biomes
{
	public class BiomeMap : ScriptableObject
	{
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
		public void GenerateChunk(ref Random random, in int2 chunk, int chunkSize, ref NativeArray<int> map)
		{
			Profiler.BeginSample("Generate Biome Map");
			for(int i = 0; i < biomes.Count; i++)
			{
				Profiler.BeginSample($"Layer {biomes[i].name}");
				biomes[i].Spawn(ref random, chunk, chunkSize, i, ref map);
				Profiler.EndSample();
			}
			Profiler.EndSample();
		}
	}
}