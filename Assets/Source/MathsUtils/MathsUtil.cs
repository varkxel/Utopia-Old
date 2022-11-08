using Unity.Burst;
using static Unity.Burst.Intrinsics.X86.Avx;
using static Unity.Burst.Intrinsics.X86.Sse2;

using Unity.Collections;

using static Unity.Mathematics.math;

namespace MathsUtils
{
	[BurstCompile]
	public static class MathsUtil
	{
		public const int MinMax_MaxBatch = 8;

		[BurstCompile]
		public static int MinMax_BatchSize()
		{
			if(IsAvxSupported) return 8;
			else if(IsSse2Supported) return 4;
			else return 1;
		}

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static unsafe void MinMax([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			if(IsAvxSupported) AVXUtils.MinMax(array, length, out minimum, out maximum);
			else if(IsSse2Supported) MinMax_Default(array, length, out minimum, out maximum);
			else MinMax_Default(array, length, out minimum, out maximum);
		}

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private static unsafe void MinMax_Default([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			// Use default code path otherwise.
			minimum = float.MaxValue;
			maximum = float.MinValue;

			for(int i = 0; i < length; i++)
			{
				float value = array[i];
				minimum = min(minimum, value);
				maximum = max(maximum, value);
			}
		}
	}
}