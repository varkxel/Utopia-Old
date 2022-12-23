using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using static Unity.Burst.Intrinsics.X86.Sse;

namespace MathsUtils {
	[BurstCompile]
	public static class SSE2Utils {
		/// <summary>
		///     The size of a batch for the <see cref="MinMax" /> function.
		/// </summary>
		public const int MinMax_batchSize = 4;

		/// <inheritdoc cref="MathsUtil.MinMax" />
		[BurstCompile]
		internal static unsafe void MinMax([ReadOnly] float* array, int length, out float minimum, out float maximum) {
			MinMax_SSE2Base(array, length, out v128 minRegister, out v128 maxRegister);

			// Reduce vectors into single values
			minimum = ReduceMin(minRegister);
			maximum = ReduceMax(maxRegister);
		}

		/// <inheritdoc cref="MathsUtil.MinMax" />
		/// <summary>
		///     SSE2 Pathway for the <see cref="MathsUtil.MinMax" /> function.
		/// </summary>
		[BurstCompile]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe void MinMax_SSE2Base([ReadOnly] float* array, int length, out v128 minRegister,
			out v128 maxRegister) {
			// Calculate the chunks to operate on, and leftovers
			int remainder = length % MinMax_batchSize;
			int lengthFloor = length - remainder;

			// Create registers
			minRegister = new v128(float.MaxValue);
			maxRegister = new v128(float.MinValue);

			// Loop through the array
			for (int offset = 0; offset < lengthFloor; offset += MinMax_batchSize) {
				// Store 4 floats from the array
				v128 valRegister = load_ps(&array[offset]);

				// Calculate the min/max for the registers
				minRegister = min_ps(minRegister, valRegister);
				maxRegister = max_ps(maxRegister, valRegister);
			}
		}

		#region Reduction

		/*
			Designed around this answer:
			https://stackoverflow.com/a/35270026
		*/

		/// <inheritdoc cref="AVXUtils.ReduceMin" />
		[BurstCompile]
		public static float ReduceMin(in v128 vec) {
			v128 shuffled = shuffle_ps(vec, vec, SHUFFLE(2, 3, 0, 1));
			v128 min = min_ps(vec, shuffled);
			shuffled = movehl_ps(shuffled, min);
			min = min_ss(min, shuffled);
			return cvtss_f32(min);
		}

		/// <inheritdoc cref="AVXUtils.ReduceMax" />
		[BurstCompile]
		public static float ReduceMax(in v128 vec) {
			v128 shuffled = shuffle_ps(vec, vec, SHUFFLE(2, 3, 0, 1));
			v128 max = max_ps(vec, shuffled);
			shuffled = movehl_ps(shuffled, max);
			max = max_ss(max, shuffled);
			return cvtss_f32(max);
		}

		#endregion
	}
}