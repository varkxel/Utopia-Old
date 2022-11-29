using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Mathematics;

using System.Collections.Generic;

namespace Utopia.World.Biomes
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
		public void GenerateChunk(in int2 chunk, int chunkSize, ref NativeArray<int> map)
		{
			Profiler.BeginSample("Generate Biome Map");
			for(int i = 0; i < biomes.Count; i++)
			{
				#if UNITY_EDITOR || DEVELOPMENT_BUILD
				// Biome null check for editor only, as it's an expensive operation.
				if(biomes[i] == null)
				{
					if(Application.isPlaying)
					{
						// Throw warning message if not in-editor.
						Debug.LogWarning($"Tried to generate chunk from {nameof(BiomeMap)} \"{name}\" with a null biome set at index {i.ToString()}.");
					}
					continue;
				}
				#endif
				
				Profiler.BeginSample($"Layer {biomes[i].name}");
				biomes[i].Spawn(chunk, chunkSize, i, ref map);
				Profiler.EndSample();
			}
			Profiler.EndSample();
		}
	}
}