using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

using static Utopia.Noise.Permutation;

namespace Utopia.Noise
{
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
	public struct SimplexFractal2D : IJobParallelFor
	{
		[ReadOnly]  public double4 bounds;
		
		[WriteOnly] public NativeArray<double> map;
		[ReadOnly]  public int2 dimensions;
		
		[ReadOnly]  public double noiseScale;
		
		[ReadOnly]  public int octaves;
		[ReadOnly]  public double amplitudeModifier;
		[ReadOnly]  public double scaleModifier;
		
		[ReadOnly]  public NativeArray<double2> offsets;
		
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static int Size
		(
			in double2 origin, in double2 size, double scale,
			out int2 dimensions, out double4 bounds
		)
		{
			bounds = double4(origin - size, origin + size);
			return Size(bounds, scale, out dimensions);
		}
		
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static int Size(in double4 bounds, double scale, out int2 dimensions)
		{
			double4 scaledBounds = bounds / scale;
			int4 boundsInt = (int4) floor(scaledBounds);
			
			dimensions = boundsInt.zw - boundsInt.xy;
			
			int total = dimensions.x * dimensions.y;
			return total;
		}
		
		[BurstCompile]
		public static void Index2D(in int2 dimensions, int index, out int2 position)
		{
			position = int2
			(
				(index) % dimensions.x,
				(index / dimensions.x) % dimensions.y
			);
		}
		
		public static SimplexFractal2D Construct
		(
			ref Random random,
			
			in double2 origin, in double2 size, double samples,
			double scale,
			int octaves, double amplitudeModifier, double scaleModifier
		)
		{
			return Construct
			(
				ref random, new double2(-double.MinValue, -double.MaxValue),
				origin, size, samples,
				scale,
				octaves, amplitudeModifier, scaleModifier
			);
		}
		
		public static SimplexFractal2D Construct
		(
			ref Random random, in double2 randomBounds,
			
			in double2 origin, in double2 size, double samples,
			double scale,
			int octaves, double amplitudeModifier, double scaleModifier
		)
		{
			int arraySize = Size(origin, size, samples, out int2 dimensions, out double4 bounds);
			SimplexFractal2D instance = new SimplexFractal2D()
			{
				bounds = bounds,
				
				dimensions = dimensions,
				map = new NativeArray<double>(arraySize, Allocator.TempJob),
				
				noiseScale = scale,
				
				octaves = octaves,
				amplitudeModifier = amplitudeModifier,
				scaleModifier = scaleModifier,
				
				offsets = new NativeArray<double2>(octaves, Allocator.TempJob)
			};
			
			for(int i = 0; i < octaves; ++i)
			{
				instance.offsets[i] = random.NextDouble2(randomBounds.xx, randomBounds.yy);
			}
			
			return instance;
		}
		
		public void Execute(int index)
		{
			Index2D(dimensions, index, out int2 sample);
			double2 position = lerp(bounds.xy, bounds.zw, (double2) sample / (double2) (dimensions - 1));
			
			double value = 0.0f;
			double amplitude = 1.0f;
			double scale = noiseScale;
			
			for(int octave = 0; octave < octaves; ++octave)
			{
				double2 samplePosition = position;
				samplePosition += offsets[octave];
				samplePosition *= scale;
				
				// value += Sample(samplePosition) * amplitude
				value = mad(Sample(samplePosition), amplitude, value);
				
				amplitude *= amplitudeModifier;
				scale *= scaleModifier;
			}
			
			map[index] = value;
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