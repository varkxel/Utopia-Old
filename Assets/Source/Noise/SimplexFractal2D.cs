using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia.Noise
{
	/// <summary>A fractal simplex 2D noise generator.</summary>
	/// <remarks>
	/// Setting <see cref="result"/> and <see cref="octaveOffsets"/>
	/// is your responsibility and must be done manually.
	/// </remarks>
	[BurstCompile(FloatPrecision.High, FloatMode.Fast)]
	public struct SimplexFractal2D : IJobParallelFor
	{
		/// <summary>
		/// Settings storage object for the fractal noise generator.
		/// Contains all the settings that need to be serialized.
		/// </summary>
		[System.Serializable]
		public struct Settings
		{
			public double scale;
			
			public int octaves;
			public double gain;
			public double lacunarity;
			
			public static Settings Default() => new Settings()
			{
				scale = 64,
				octaves = 5,
				gain = 0.5,
				lacunarity = 2.0
			};
		}
		
		public Settings settings;
		
		public int2 index;
		public int size;
		
		// Cached total amplitude
		private const double initialAmplitude = 0.5;
		private double amplitudeTotal;
		
		/// <summary>The position offsets to use for each octave.</summary>
		/// <remarks>It is your responsibility to set these before executing the generator.</remarks>
		[ReadOnly] public NativeArray<double2> octaveOffsets;
		
		/// <summary>The array containing the results of the generator.</summary>
		/// <remarks>You need to allocate the result vector yourself.</remarks>
		[WriteOnly] public NativeArray<double> result;
		
		/// <summary>Initialises local cached variables used within the job.</summary>
		/// <remarks>You don't need to call this if you created your job via a NoiseMap2D object.</remarks>
		public void Initialise()
		{
			// Calculate total amplitude for normalisation
			double amplitude = initialAmplitude;
			amplitudeTotal = amplitude;
			for(int i = 0; i < settings.octaves - 1; i++)
			{
				amplitude *= settings.gain;
				amplitudeTotal += amplitude;
			}
		}
		
		/// <summary>Cached per-octave rotation matrix.</summary>
		/// <remarks>Used to reduce artefacts in the result.</remarks>
		private static readonly double2x2 rotation = new double2x2
		(
			cos(0.5), sin(0.5),
			-sin(0.5), cos(0.5)
		);
		
		public void Execute(int i)
		{
			// Get sample position
			double2 position = double2(0.0);
			position += double2(index) * (double) size;
			// ReSharper disable once PossibleLossOfFraction
			position += double2(i % size, i / size);
			position /= settings.scale;
			
			// Fractal noise algorithm
			double value = 0.0;
			double amplitude = initialAmplitude;
			double frequency = 2.0;
			
			for(int octave = 0; octave < settings.octaves; octave++)
			{
				// Permute the position and use the given offset
				position = mul(rotation, position) * frequency + octaveOffsets[octave];
				
				// Sample octave value
				value += amplitude * Sample(position);
				
				// Update modifiers
				frequency *= settings.lacunarity;
				amplitude *= settings.gain;
			}
			
			// Normalise
			value /= amplitudeTotal;
			
			result[i] = value;
		}
		
		#region Noise Algorithm
		
		/*
			This noise algorithm is based on this implementation:
			https://www.shadertoy.com/view/4dS3Wd
		*/
		
		[BurstCompile]
		private static double Hash(in double2 val)
		{
			double3 val3D = frac(val.xyx * 0.13);
			val3D += dot(val3D, val3D.yzx + 3.333);
			return frac((val3D.x + val3D.y) * val3D.z);
		}
		
		[BurstCompile(FloatPrecision.High, FloatMode.Fast)]
		public static double Sample(in double2 position)
		{
			double2 integer = floor(position);
			double2 fraction = frac(position);
			
			double a = Hash(integer);
			double b = Hash(integer + double2(1, 0));
			double c = Hash(integer + double2(0, 1));
			double d = Hash(integer + double2(1, 1));
			
			// 2D lerp between values with smoothstep
			double2 u = fraction * fraction * (3.0 - 2.0 * fraction);
			return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
		}
		
		#endregion
	}
}