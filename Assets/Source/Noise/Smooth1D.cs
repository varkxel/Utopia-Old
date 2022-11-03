using Unity.Burst;
using static Unity.Mathematics.math;

namespace Utopia.Noise
{
	[BurstCompile]
	public static class Smooth1D
	{
		/// <summary>
		/// Single-dimension random function for the noise creation.
		/// </summary>
		/// <param name="x">1D position input.</param>
		/// <returns>Random value at the position.</returns>
		[BurstCompile]
		public static float Random(float x) => frac(sin(x) * 100000.0f);

		[BurstCompile]
		public static float Sample(float x)
		{
			float integer = floor(x);
			float fraction = frac(x);

			// Interpolate between the two samples by the fractional component with smoothing.
			return lerp(Random(integer), Random(integer + 1.0f), smoothstep(0.0f, 1.0f, fraction));
		}

		[BurstCompile]
		public static float Fractal(float x, uint octaves, float lacunarity, float gain)
		{
			float value = 0.0f;
			float amplitude = 0.5f;
			float frequency = 1.0f;

			for(uint i = 0; i < octaves; i++)
			{
				value += amplitude * Sample(frequency * x);
				frequency *= lacunarity;
				amplitude *= gain;
			}
			return value;
		}
	}
}