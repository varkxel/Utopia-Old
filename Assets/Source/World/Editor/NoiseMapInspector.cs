using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

using Utopia.Noise;

namespace Utopia.World
{
	/// <summary>
	/// An inspector to visualise the Noise Maps.
	/// </summary>
	[CustomEditor(typeof(NoiseMap2D))]
	internal sealed class NoiseMapInspector : TexturePreviewInspector
	{
		/// <summary>
		/// Random instance to be used for the preview generation.
		/// </summary>
		private Random random;
		
		protected override void Awake()
		{
			random.InitState();
			
			base.Awake();
		}
		
		public override void UpdateTexture()
		{
			NoiseMap2D noiseMap = target as NoiseMap2D;
			Debug.Assert(noiseMap != null, nameof(noiseMap) + " != null");
			
			// Initialise the noise map
			noiseMap.GenerateOffsets(ref random, persistent: false);
			noiseMap.CreateJob(new int2(0, 0), resolution, out SimplexFractal2D generator);
			
			// Generate the noise map to display
			const int resultLength = resolution * resolution;
			NativeArray<double> result = new NativeArray<double>(resultLength, Allocator.TempJob);
			generator.result = result;
			
			JobHandle generatorHandle = generator.Schedule(resultLength, 4);
			
			// Allocate managed array while job is running
			Color[] image = new Color[resolution * resolution];
			
			// Await job to finish
			generatorHandle.Complete();
			noiseMap.DestroyOffsets();
			
			// Convert doubles to float for texture
			for(int i = 0; i < result.Length; i++)
			{
				float value = (float) result[i];
				image[i] = new Color(value, value, value, 1.0f);
			}
			
			// Free noisemap
			result.Dispose();
			
			// Apply result
			UploadTexture(image);
		}
	}
}