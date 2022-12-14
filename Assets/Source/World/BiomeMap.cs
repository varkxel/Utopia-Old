using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

using System.Collections.Generic;
using MathsUtils;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Events;

namespace Utopia.World
{
	[CreateAssetMenu(menuName = AssetPath + "Biome Map", fileName = "Biome Map")]
	public class BiomeMap : ScriptableObject, System.IDisposable
	{
		internal const string AssetPath = Generator.AssetPath + "Biomes/";

		[Header("Biome List")]
		public List<Biome> biomes = new List<Biome>();

		[Header("Blending")]
		[Range(0.0f, 1.0f)] public float blendThreshold = 0.1f;

		public NativeArray<Curve.RawData> curves;

		public void Initialise()
		{
			curves = new NativeArray<Curve.RawData>(biomes.Count, Allocator.Persistent);
			for(int i = 0; i < biomes.Count; i++)
			{
				biomes[i].Initialise();
				curves[i] = biomes[i].heightmapModifier.GetRawData();
			}
		}

		public void Dispose()
		{
			curves.Dispose();
			
			// Free biome data
			foreach(Biome biome in biomes)
			{
				biome.Dispose();
			}
		}

		public Biome[] GetBiomes(float4 map)
		{
			map = trunc(map);
			Biome[] result = new Biome[4];
			for(int i = 0; i < 4; i++)
			{
				result[i] = biomes[(int) map[i]];
			}
			return result;
		}

		private NativeArray<double> weightingData;

		public void GenerateChunk_OnComplete()
		{
			weightingData.Dispose();
			for(int i = 0; i < biomes.Count; i++)
			{
				biomes[i].OnComplete();
			}
		}
		
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
			bool persistent = false
		)
		{
			int arrayLength = chunkSize * chunkSize;
			map = new NativeArray<float4>
			(
				arrayLength,
				persistent ? Allocator.Persistent : Allocator.TempJob
			);
			
			int biomeCount = biomes.Count;

			weightingData = new NativeArray<double>(arrayLength * biomeCount, Allocator.TempJob);

			JobHandle? previous = null;
			for(int i = 0; i < biomeCount; i++)
			{
				#if UNITY_EDITOR || DEVELOPMENT_BUILD
				// Biome null check for editor/development only, as it's an expensive operation.
				if(biomes[i] == null)
				{
					// Throw warning message if not in-editor.
					throw new System.NullReferenceException($"Tried to generate chunk from {nameof(BiomeMap)} \"{name}\" with a null biome set at index {i.ToString()}.");
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

				float4 comparison = new float4(clamp((float) biomeWeighting[index], 0.0f, 1.0f - EPSILON));
				bool4 isGreater = comparison > weights;
				bool4 smallestWeight = abs(weights - cmin(weights)) < EPSILON;

				bool4 replace = isGreater & smallestWeight;
				bool replaced = false;
				for(int i = 0; i < 4; i++)
				{
					replace[i] &= !replaced;
					replaced |= replace[i];
				}

				result = select(result, comparison, replace);

				float4 biomeIndexVec = new float4(biomeIndex);
				float4 replaceMask = (float4) replace;
				biomeIndexVec *= replaceMask;
				result += biomeIndexVec;

				biomes[index] = result;
			}
		}

		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		public struct ModifierJob : IJobParallelFor
		{
			[ReadOnly] public NativeArray<float4> biomes;
			[ReadOnly] public NativeArray<Curve.RawData> curves;

			public NativeArray<double> heightmap;

			public float blend;

			public void Execute(int index)
			{
				float4 biomeSample = biomes[index];

				// Get curve references
				int4 biomeIndex = (int4) trunc(biomeSample);
				NativeArray<Curve.RawData> biomeCurves = new NativeArray<Curve.RawData>(4, Allocator.Temp);
				for(int i = 0; i < 4; i++)
				{
					Loop.ExpectVectorized();
					biomeCurves[i] = curves[biomeIndex[i]];
				}

				// Sample the curves
				float4 samples = new float4();
				for(int i = 0; i < 4; i++)
				{
					samples[i] = Curve.Evaluate((float) heightmap[index], biomeCurves[i]);
				}

				float4 weights = frac(biomeSample);

				float maxWeight = cmax(weights);
				float4 difference = maxWeight - weights;

				// Calculate which values should be blended between
				bool4 shouldBlend = difference <= blend;

				weights = unlerp(blend, 0.0f, difference);

				// Exclude out of threshold values
				weights *= (float4) shouldBlend;

				// Blend by weights
				samples *= weights;
				float val = csum(samples);
				val /= csum(weights);
				heightmap[index] = val;
			}
		}
	}
}