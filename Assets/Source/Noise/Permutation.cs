using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia.Noise
{
	[BurstCompile]
	internal static class Permutation
	{
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static void Mod289(ref double2 vec)
		{
			vec -= floor(vec * (1.0 / 289.0)) * 289.0;
		}
		
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static void Mod289(ref double3 vec)
		{
			vec -= floor(vec * (1.0 / 289.0)) * 289.0;
		}
		
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static void Mod289(ref double4 vec)
		{
			vec -= floor(vec * (1.0 / 289.0)) * 289.0;
		}
		
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static void Permute(ref double4 vec)
		{
			vec = ((vec * 34.0) + 10.0) * vec;
			Mod289(ref vec);
		}
		
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static void Permute(ref double3 vec)
		{
			vec = ((vec * 34.0) + 10.0) * vec;
			Mod289(ref vec);
		}
	}
}