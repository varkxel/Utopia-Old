using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

using Utopia.Noise;

namespace Utopia.World
{
	[CreateAssetMenu(menuName = Generator.AssetPath + "Noise Map", fileName = "Noise Map")]
	public class NoiseMap2D : ScriptableObject
	{
		// Serialized data
		public SimplexFractal2D.Settings settings = SimplexFractal2D.Settings.Default();
		
		// Context data
		[System.NonSerialized]
		public NativeArray<double2> octaveOffsets;
		
		/// <summary>
		/// Generates the offsets for the noise map to use.
		/// </summary>
		/// <param name="random">Random instance to utilise.</param>
		/// <param name="range">
		/// Range of offsets to generate:
		/// Higher is more random, lower has less chance of artifacts.
		/// </param>
		public void GenerateOffsets(ref Random random, double range = 64000.0)
		{
			int octaves = settings.octaves;
			
			octaveOffsets = new NativeArray<double2>(octaves, Allocator.Persistent);
			for(int octave = 0; octave < octaves; octave++)
			{
				octaveOffsets[octave] = random.NextDouble2(-range, range);
			}
		}
		
		public void OnDestroy()
		{
			octaveOffsets.Dispose();
		}
		
		/// <summary>
		/// Creates a job with all parameters set besides the result array.
		/// </summary>
		/// <param name="chunk"></param>
		/// <param name="chunkSize"></param>
		/// <param name="job"></param>
		public void CreateJob(in int2 chunk, int chunkSize, out SimplexFractal2D job)
		{
			job = new SimplexFractal2D()
			{
				index = chunk,
				size = chunkSize,
				
				settings = this.settings,
				octaveOffsets = this.octaveOffsets
			};
		}
	}
}