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
		
		private NativeList<JobHandle> handles;
		
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
			int biomeCount = biomes.Count;

			handles = new NativeList<JobHandle>(biomeCount, Allocator.TempJob);
			for(int i = 0; i < biomeCount; i++)
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
					return;
				}
				#endif

				JobHandle? previous = i > 0 ? handles[i - 1] : null;
				handles.Add(biomes[i].Spawn(chunk, chunkSize, i, map, previous));
			}
		}

		public bool IsComplete()
		{
			for(int i = 0; i < handles.Length; i++)
			{
				if(!handles[i].IsCompleted) return false;
			}
			return true;
		}

		public void OnCompleted()
		{
			handles.Dispose();
			
			// Free biome data
			for(int i = 0; i < biomes.Count; i++)
			{
				biomes[i].OnCompleted();
			}
		}
	}
}