using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

using Utopia.Noise;

namespace Utopia.World
{
	/// <summary>
	/// A noise map container which handles creation of,
	/// variables for and cleanup of a <see cref="SimplexFractal2D"/> noise map.
	/// </summary>
	[CreateAssetMenu(menuName = Generator.AssetPath + "Noise Map", fileName = "Noise Map")]
	public class NoiseMap2D : ScriptableObject
	{
		/// <summary>
		/// The serialised parameters / settings for the noise map generation.
		/// </summary>
		public SimplexFractal2D.Settings settings = SimplexFractal2D.Settings.Default();
		
		// Context data
		[System.NonSerialized] public NativeArray<double2> octaveOffsets;
		[System.NonSerialized] public bool octaveOffsetsGenerated = false;
		
		public void GenerateOffsets(double range = 64000.0)
		{
			// Generate offsets by default on creation
			GenerateOffsets(ref Generator.instance.random, persistent: true, range);
		}
		
		/// <summary>
		/// Generates the offsets for the noise map to use.
		/// </summary>
		/// <param name="random">Random instance to utilise.</param>
		/// <param name="persistent">
		/// True if the result array should be allocated as <see cref="Allocator.Persistent"/>,
		/// or false if it should be allocated as <see cref="Allocator.TempJob"/>.
		/// </param>
		/// <param name="range">
		/// Range of offsets to generate:
		/// Higher is more random, lower has less chance of artifacts.
		/// </param>
		public void GenerateOffsets(ref Random random, bool persistent, double range = 64000.0)
		{
			if(octaveOffsetsGenerated) return;
			octaveOffsetsGenerated = true;
			
			int octaves = settings.octaves;
			
			octaveOffsets = new NativeArray<double2>(octaves, persistent ? Allocator.Persistent : Allocator.TempJob);
			for(int octave = 0; octave < octaves; octave++)
			{
				octaveOffsets[octave] = random.NextDouble2(-range, range);
			}
		}
		
		public void DestroyOffsets()
		{
			if(!octaveOffsetsGenerated) return;

			octaveOffsets.Dispose();
			octaveOffsetsGenerated = false;
		}
		
		/// <summary>
		/// Creates and initialises a <see cref="SimplexFractal2D"/> job for a given chunk,
		/// with all parameters set besides the <see cref="SimplexFractal2D.result"/> array.
		/// </summary>
		/// <param name="chunk">The chunk index to generate.</param>
		/// <param name="chunkSize">The size of the chunk to generate.</param>
		/// <param name="job">The job created.</param>
		public void CreateJob(in int2 chunk, int chunkSize, out SimplexFractal2D job)
		{
			int2 index = chunk * chunkSize;
			CreateJob(new int4(index, index + chunkSize), out job);
		}
		
		/// <summary>
		/// Creates and initialises a <see cref="SimplexFractal2D"/> job for the given bounds,
		/// with all parameters set besides the <see cref="SimplexFractal2D.result"/> array.
		/// </summary>
		/// <param name="bounds">The bounds for the sample to generate.</param>
		/// <param name="job">The job created.</param>
		public void CreateJob(in int4 bounds, out SimplexFractal2D job)
		{
			GenerateOffsets();
			job = new SimplexFractal2D()
			{
				bounds = bounds,
				settings = this.settings,
				octaveOffsets = this.octaveOffsets
			};
			job.Initialise();
		}
	}
}