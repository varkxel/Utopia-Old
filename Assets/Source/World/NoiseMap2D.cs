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
		
		protected virtual void Awake()
		{
			// Generate offsets by default on creation
			GenerateOffsets(ref Generator.instance.random, persistent: true);
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
			int octaves = settings.octaves;
			
			octaveOffsets = new NativeArray<double2>(octaves, persistent ? Allocator.Persistent : Allocator.TempJob);
			for(int octave = 0; octave < octaves; octave++)
			{
				octaveOffsets[octave] = random.NextDouble2(-range, range);
			}
		}
		
		public virtual void OnDestroy()
		{
			// Destroy offsets on destruction
			octaveOffsets.Dispose();
		}
		
		/// <summary>
		/// Creates and initialises a <see cref="SimplexFractal2D"/> job,
		/// with all parameters set besides the <see cref="SimplexFractal2D.result"/> array.
		/// </summary>
		/// <param name="chunk">The chunk index to generate.</param>
		/// <param name="chunkSize">The size of the chunk to generate.</param>
		/// <param name="job">The job created.</param>
		public void CreateJob(in int2 chunk, int chunkSize, out SimplexFractal2D job)
		{
			job = new SimplexFractal2D()
			{
				index = chunk,
				size = chunkSize,
				
				settings = this.settings,
				octaveOffsets = this.octaveOffsets
			};
			job.Initialise();
		}
	}
}