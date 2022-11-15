using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

using static Utopia.Noise.Permutation;

namespace Utopia.Noise
{
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast), System.Serializable]
	public struct SimplexFractal2D : IJobParallelFor
	{
		[ReadOnly] public NativeArray<double2> octaveOffsets;
		[WriteOnly] public NativeArray<double> result;

		[HideInInspector] public double2 origin;
		[HideInInspector] public int2 index;
		[HideInInspector] public int size;

		public double scale;

		public int octaves;
		public double gain;
		public double lacunarity;

		private static readonly double2x2 rotation = new double2x2
		(
			cos(0.5), sin(0.5),
			-sin(0.5), cos(0.5)
		);

		public void Execute(int i)
		{
			/*
				Designed around fractal brownian motion from:
				https://thebookofshaders.com/13/
			*/
			double2 position = origin;
			position += double2(index) * (double) size;
			// ReSharper disable once PossibleLossOfFraction
			position += double2(i % size, i / size);

			double value = 0.0f;
			double amplitude = 0.5f;
			double frequency = scale;

			for(int octave = 0; octave < octaves; octave++)
			{
				position = mul(rotation, position) * frequency + octaveOffsets[octave];
				value += amplitude * Sample(position);
				amplitude *= gain;
				frequency *= lacunarity;
			}
			result[i] = value;
		}

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static double Sample(in double2 position)
		{
			/*
				This method is based on this implementation:
				https://github.com/ashima/webgl-noise/blob/master/src/noise2D.glsl
			*/

			double C_x = (3.0 - sqrt(3.0)) / 6.0;
			double C_y = 0.5 * (sqrt(3.0) - 1.0);
			double C_z = -1.0 + 2.0 * C_x;
			const double C_w = 1.0 / 41.0;

			double4 C = double4(C_x, C_y, C_z, C_w);

			// First corner
			double2 i = floor(position + dot(position, C.yy));
			double2 x0 = position - dot(i, C.xx);

			// Other corners
			/*
				Branch-less implementation from:
				https://www.arxiv-vanity.com/papers/1204.1461/
			*/
			double2 i1 = double2(0.0, 0.0);
			i1.x = step(x0.y, x0.x);
			i1.y = 1.0 - i1.x;

			double4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;

			// Permutations
			Mod289(ref i);
			double3 p = i.y + double3(0.0, i1.y, 1.0);
			Permute(ref p);
			p += i.x + double3(0.0, i1.x, 1.0);
			Permute(ref p);

			double3 m = max(0.5 - double3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
			m *= m;
			m *= m;

			// Gradients: 41 points uniformly over a line, mapped onto a diamond.
			// The ring size 17 * 17 = 289 is close to a multiple of 41 (41 * 7 = 287)

			double3 x = 2.0 * frac(p * C.www) - 1.0;
			double3 h = abs(x) - 0.5;
			double3 ox = floor(x + 0.5);
			double3 a0 = x - ox;

			// Normalise gradients implicitly by scaling m
			m *= rsqrt(a0 * a0 + h * h);

			// Compute final noise value at P
			double3 g = double3
			(
				// X
				a0.x * x0.x + h.x * x0.y,
				// YZ
				a0.yz * x12.xz + h.yz * x12.yw
			);
			return 130.0 * dot(m, g);
		}
	}
}