using Unity.Burst;
using Unity.Burst.Intrinsics;
using static Unity.Burst.Intrinsics.X86.Sse;
using static Unity.Burst.Intrinsics.X86.Sse3;

using Unity.Collections;

namespace MathsUtils
{
	[BurstCompile]
	public static class SSE4Utils
	{
		#region Reduction
		
		/*
			Designed around this answer:
			https://stackoverflow.com/a/35270026
		*/
		
		/// <inheritdoc cref="AVXUtils.ReduceMin"/>
		[BurstCompile]
		public static float ReduceMin(in v128 vec)
		{
			v128 shuffled = movehdup_ps(vec);
			v128 min = min_ps(vec, shuffled);
			shuffled = movehl_ps(shuffled, min);
			min = min_ss(min, shuffled);
			return cvtss_f32(min);
		}
		
		/// <inheritdoc cref="AVXUtils.ReduceMax"/>
		[BurstCompile]
		public static float ReduceMax(in v128 vec)
		{
			v128 shuffled = movehdup_ps(vec);
			v128 max = max_ps(vec, shuffled);
			shuffled = movehl_ps(shuffled, max);
			max = max_ss(max, shuffled);
			return cvtss_f32(max);
		}
		
		#endregion

		/// <inheritdoc cref="MathsUtil.MinMax"/>
		/// <summary>
		/// SSE4 Pathway for the <see cref="MathsUtil.MinMax"/> function.
		/// </summary>
		[BurstCompile]
		internal static unsafe void MinMax([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			SSE2Utils.MinMax_SSE2Base(array, length, out v128 minRegister, out v128 maxRegister);

			// Reduce vectors into single values
			minimum = ReduceMin(minRegister);
			maximum = ReduceMax(maxRegister);
		}
	}
}