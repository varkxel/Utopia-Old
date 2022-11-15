using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

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

			double value = 0.0;
			double amplitude = 0.5;
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

		#region Noise Algorithm

		/*
			This noise algorithm is based on this implementation:
			https://www.shadertoy.com/view/4dS3Wd
		*/

		[BurstCompile]
		private static double Hash(double val)
		{
			val = frac(val * 0.011);
			val *= val + 7.5;
			val *= val + val;
			return frac(val);
		}

		[BurstCompile]
		private static double Hash(double2 val)
		{
			double3 val3D = frac(val.xyx * 0.13);
			val3D += dot(val3D, val3D.yzx + 3.333);
			return frac((val3D.x + val3D.y) * val3D.z);
		}

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
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