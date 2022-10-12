using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;

using static Utopia.Noise.Permutation;

namespace Utopia.Noise
{
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
	public static class Simplex3D
	{
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static double Sample(in double3 position)
		{
			/* 
				This method is based on this implementation:
				https://github.com/ashima/webgl-noise/blob/master/src/noise3D.glsl
			*/
			
			const double C_x = 1.0 / 6.0;
			const double C_y = 1.0 / 3.0;
			double2 C = double2(C_x, C_y);
			double4 D = double4(0.0, 0.5, 1.0, 2.0);
			
			// First corner
			double3 i = floor(position + dot(position, C.yyy));
			double3 x0 = position - i + dot(i, C.xxx);
			
			// Other corners
			double3 g = step(x0.yzx, x0.xyz);
			double3 l = 1.0 - g;
			double3 i1 = min(g.xyz, l.zxy);
			double3 i2 = max(g.xyz, l.zxy);
			
			double3 x1 = x0 - i1 + C.xxx;
			double3 x2 = x0 - i2 + C.yyy;
			double3 x3 = x0 - D.yyy;
			
			// Permutations
			Mod289(ref i);
			double4 p = i.z + double4(0.0, i1.z, i2.z, 1.0);
			Permute(ref p);
			p += i.y + double4(0.0, i1.y, i2.y, 1.0);
			Permute(ref p);
			p += i.x + double4(0.0, i1.x, i2.x, 1.0);
			Permute(ref p);
			
			// Gradients 7x7 points over a square, mapped onto an octahedron.
			// The ring size 17 * 17 = 289 is close to a multiple of 49 (49 * 6 = 294)
			const double n_ = 1.0 / 7.0;
			double3 ns = n_ * D.wyz - D.xzx;
			
			double4 j = p - 49.0 * floor(p * ns.z * ns.z); // mod(p, 7*7)
			
			double4 x_ = floor(j * ns.z);
			double4 y_ = floor(j - 7.0 * x_); // mod(j, N)
			
			double4 x = x_ * ns.x + ns.yyyy;
			double4 y = y_ * ns.x + ns.yyyy;
			double4 h = 1.0 - abs(x) - abs(y);
			
			double4 b0 = double4(x.xy, y.xy);
			double4 b1 = double4(x.zw, y.zw);
			
			double4 s0 = floor(b0) * 2.0 + 1.0;
			double4 s1 = floor(b1) * 2.0 + 1.0;
			double4 sh = -step(h, double4(0.0));
			
			double4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
			double4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
			
			double3 p0 = double3(a0.xy, h.x);
			double3 p1 = double3(a0.zw, h.y);
			double3 p2 = double3(a1.xy, h.z);
			double3 p3 = double3(a1.zw, h.w);
			
			// Normalise Gradients
			double4 normal = rsqrt(double4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
			p0 *= normal.x;
			p1 *= normal.y;
			p2 *= normal.z;
			p3 *= normal.w;
			
			// Mix final noise value
			double4 m = max(0.5 - double4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
			m *= m;
			return 105.0 * dot(m * m, double4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
		}
	}
}