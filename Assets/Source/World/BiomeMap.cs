using UnityEngine;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

using System.Collections.Generic;

using MathsUtils;


namespace Utopia.World
{
	/// <summary>
	/// A set of biomes that can be used to generate a world,
	/// along with a set of parameters to do so.
	/// </summary>
	[CreateAssetMenu(menuName = AssetPath + "Biome Map", fileName = "Biome Map")]
	public class BiomeMap : ScriptableObject, System.IDisposable
	{
		/// <summary>
		/// Asset menu path for this object and subobjects.
		/// </summary>
		internal const string AssetPath = Generator.AssetPath + "Biomes/";

		/// <summary>
		/// Biome Texture shader property.
		/// </summary>
		private static readonly int BiomeTextures = Shader.PropertyToID("_BiomeTextures");

		/// <summary>
		/// The ordered biome list for the biome map to be generated with.
		/// </summary>
		[Header("Biome List")]
		public List<Biome> biomes = new List<Biome>();

		// TODO Find a way to encapsulate.
		/// <summary>
		/// The raw curve data to be used
		/// </summary>
		internal NativeArray<Curve.RawData> curves;

		/// <summary>
		/// The texture array that all the biome texture get added to
		/// and gets uploaded to the GPU.
		/// </summary>
		[System.NonSerialized] public Texture2DArray textures;

		/// <summary>
		/// The size of the biome textures.
		/// </summary>
		[Header("Texturing")]
		public int textureSize = 2048;

		/// <summary>
		/// Initialises the biome map and all sub-objects / parameters
		/// for use and generation.
		/// </summary>
		public void Initialise()
		{
			// Initialise curves & GPU textures
			curves = new NativeArray<Curve.RawData>(biomes.Count, Allocator.Persistent);
			textures = new Texture2DArray
			(
				// Dimensions - X, Y, Z (Length)
				textureSize, textureSize, biomes.Count,
				
				// Initialise to texture format of the first biome's texture.
				// All textures *should* be the same.
				biomes[0].biomeTexture.format,
				
				// Generate mipmaps
				true
			);

			for(int i = 0; i < biomes.Count; i++)
			{
				// Initialise biomes & biome curves
				biomes[i].Initialise();
				curves[i] = biomes[i].heightmapModifier.GetRawData();
				
				// Initialise biome textures / upload textures to GPU
				Graphics.CopyTexture(biomes[i].biomeTexture, 0, textures, i);
			}

			// Bind texture array for the shader to use.
			Shader.SetGlobalTexture(BiomeTextures, textures);
		}

		/// <summary>Frees all utilised memory from the biome map.</summary>
		/// <remarks>Call once the biome map is no longer needed.</remarks>
		public void Dispose()
		{
			curves.Dispose();
			foreach(Biome biome in biomes)
			{
				biome.Dispose();
			}
		}

		/// <summary>
		/// Internal storage for the weights for each biome in the current chunk.
		/// </summary>
		private NativeArray<double> weightingData;

		/// <summary>
		/// Generates a chunk for the map using the stored biome spawn rule list.
		/// </summary>
		/// <param name="chunk">The chunk index to generate.</param>
		/// <param name="chunkSize">The size of the chunk to generate.</param>
		/// <param name="map">
		/// The map to generate the biome map into.
		/// Needs to be chunkSize * chunkSize length.
		/// </param>
		/// <param name="persistent">
		/// Whether the generated biome map for the chunk should be allocated in persistent memory
		/// or temporary memory.
		/// </param>
		public JobHandle GenerateChunk
		(
			in int2 chunk, int chunkSize, out NativeArray<float4> map,
			bool persistent = false
		)
		{
			// Initialisation
			int biomeCount = biomes.Count;
			
			int arrayLength = chunkSize * chunkSize;
			map = new NativeArray<float4>
			(
				arrayLength,
				persistent ? Allocator.Persistent : Allocator.TempJob
			);
			
			weightingData = new NativeArray<double>(arrayLength * biomeCount, Allocator.TempJob);

			// Loop through the biome array, scheduling all required jobs for each biome
			// and taking in the previous biome as a dependency - since they all write to the same output map.
			JobHandle? previous = null;
			for(int i = 0; i < biomeCount; i++)
			{
				#region Editor Safety Check
				#if UNITY_EDITOR || DEVELOPMENT_BUILD
				// Biome null check for editor/development only, as it's an expensive operation.
				if(biomes[i] == null)
				{
					// Throw warning message if not in-editor.
					throw new System.NullReferenceException($"Tried to generate chunk from {nameof(BiomeMap)} \"{name}\" with a null biome set at index {i.ToString()}.");
				}
				#endif
				#endregion

				// Get the current biome's slice from the weighting data array.
				NativeSlice<double> weighting = weightingData.Slice(i * arrayLength, arrayLength);

				// Create the weighting job.
				JobHandle weightingJob = biomes[i].CalculateWeighting(chunk, chunkSize, weighting);

				// Create the biome map packing job.
				PackJob packJob = new PackJob()
				{
					biomeIndex = i,
					biomeWeighting = weighting,
					biomes = map
				};

				// Calculate the dependencies for the job.
				JobHandle dependency = weightingJob;
				if(previous != null) dependency = JobHandle.CombineDependencies(dependency, previous.Value);

				// Schedule the job.
				JobHandle packJobHandle = packJob.Schedule(arrayLength, 4, dependency);
				
				// Store the JobHandle for the next job to use as a dependency.
				previous = packJobHandle;
			}

			// Return the last JobHandle.
			return previous ?? default;
		}

		/// <summary>
		/// Called once GenerateChunk() has been completed.
		/// This is to dispose of temporary arrays generated during the job.
		/// </summary>
		public void GenerateChunk_OnComplete()
		{
			weightingData.Dispose();
			foreach(Biome biome in biomes)
			{
				biome.OnComplete();
			}
		}

		/// <summary>
		/// Internal job to pack the given biome into the biome map.
		/// </summary>
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private struct PackJob : IJobParallelFor
		{
			// Current biome parameters
			public int biomeIndex;
			[ReadOnly] public NativeSlice<double> biomeWeighting;

			// Generated biome map
			public NativeArray<float4> biomes;

			public void Execute(int index)
			{
				// Load in the current sample
				float4 result = biomes[index];

				// Get weights from sample
				float4 weights = result;
				weights = frac(weights);

				// Load in the current weight as a float4 as a comparison vector to the sample
				float currentWeight = clamp((float) biomeWeighting[index], 0.0f, 1.0f - EPSILON);
				float4 comparison = new float4(currentWeight);

				// Calculate the smallest weight as a mask of the vector (preserve vectorisation)
				bool4 smallestWeight = cmin(weights) == weights;

				// Apply the smallest weight mask to the current comparison vector
				comparison *= (float4) smallestWeight;

				// Check which weights are smaller than the current comparison vector.
				// The comparison is >= to always set where possible to populate the result vector
				// with as many different biome indices as possible.
				bool4 isGreater = comparison >= weights;

				// Find the value in the sample array to replace with this biome (if any).
				bool4 replace = isGreater & smallestWeight;
				MathsUtil.Unique(replace, out replace);

				// Create the replacement biome entry and broadcast to all 4 elements in a vector
				float4 replacement = comparison + biomeIndex;

				// Swap the sample in the biome map where it should be replaced.
				result = select(result, replacement, replace);

				// Set the biome map sample
				biomes[index] = result;
			}
		}

		/// <summary>
		/// Job to apply the heightmap modifiers from the biomes.
		/// </summary>
		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		internal struct ModifierJob : IJobParallelFor
		{
			// Biome map & biome curve data
			[ReadOnly] public NativeArray<float4> biomes;
			[ReadOnly] public NativeArray<Curve.RawData> curves;

			// Heightmap to modify by the biome multiplier
			public NativeArray<double> heightmap;

			public void Execute(int index)
			{
				float4 biomeSample = biomes[index];

				// Get raw curve data
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

				// Calculate weights and normalise them between 0 and 1
				float4 weights = frac(biomeSample);
				weights /= csum(weights);

				// Weight the samples
				for(int i = 0; i < 4; i++)
				{
					Loop.ExpectVectorized();
					samples[i] *= weights[i];
				}

				// Calculate the result by adding the weighted samples
				float result = csum(samples);
				heightmap[index] = result;
			}
		}
	}
}