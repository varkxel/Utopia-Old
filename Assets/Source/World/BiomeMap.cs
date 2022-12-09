using System;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

using System.Collections.Generic;
using MathsUtils;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Events;

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
		public JobHandle GenerateChunk
		(
			in int2 chunk, int chunkSize, out NativeArray<float4> map,
			out UnityAction completionCallback, bool persistent = false
		)
		{
			int arrayLength = chunkSize * chunkSize;
			map = new NativeArray<float4>
			(
				arrayLength,
				persistent ? Allocator.Persistent : Allocator.TempJob,
				NativeArrayOptions.UninitializedMemory
			);
			for(int i = 0; i < arrayLength; i++) map[i] = -1.0f;
			
			int biomeCount = biomes.Count;

			NativeArray<double> weightingData = new NativeArray<double>(arrayLength * biomeCount, Allocator.TempJob);
			
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

				NativeSlice<double> weighting = weightingData.Slice(i * arrayLength, arrayLength);
				JobHandle weightingJob = biomes[i].CalculateWeighting(chunk, chunkSize, weighting);

				PackJob packJob = new PackJob()
				{
					biomeIndex = i,
					biomeWeighting = weighting,
					biomes = map
				};
				JobHandle dependency = weightingJob;
				if(previous != null) dependency = JobHandle.CombineDependencies(dependency, previous.Value);

				JobHandle packJobHandle = packJob.Schedule(arrayLength, 4, dependency);
				previous = packJobHandle;
			}

			completionCallback = () => weightingData.Dispose();
			return previous ?? default;
		}

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private struct PackJob : IJobParallelFor
		{
			public int biomeIndex;
			[ReadOnly] public NativeSlice<double> biomeWeighting;

			public NativeArray<float4> biomes;

			public void Execute(int index)
			{
				float4 result = biomes[index];
				float4 weights = result;
				weights = frac(weights);

				float4 comparison = new float4(biomeWeighting[index]);
				bool4 isGreater = comparison > weights;
				bool4 smallestWeight = abs(weights - MathsUtil.MinItem(weights)) < EPSILON;

				bool4 replace = isGreater & smallestWeight;
				result = select(result, comparison, replace);

				float4 biomeIndexVec = new float4(biomeIndex);
				float4 replaceMask = (float4) replace;
				biomeIndexVec *= replaceMask;
				result += biomeIndexVec;

				biomes[index] = result;
			}
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