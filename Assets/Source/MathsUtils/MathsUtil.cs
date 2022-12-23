using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace MathsUtils {
	/// <summary>
	///     A set of miscellaneous utilities that are commonly used across the project.
	/// </summary>
	[BurstCompile]
	public static class MathsUtil {
		/// <summary>
		///     Clears a vector in order to only have one member be true.
		/// </summary>
		/// <param name="vec">The vector to make unique.</param>
		/// <param name="result">The vector to write the result to.</param>
		[BurstCompile]
		public static void Unique(in bool4 vec, out bool4 result) {
			result = vec;

			// TODO Find faster approach.
			bool replaced = false;
			for (int i = 0; i < 4; i++) {
				result[i] &= !replaced;
				replaced |= result[i];
			}
		}

		#region MinMax

		/// <summary>
		///     The minimum batch size that can be used for the <see cref="MinMax" /> function.
		///     All given data needs to be a multiple of this value.
		/// </summary>
		/// <remarks>
		///     See <see cref="MinMax_BatchSize" /> for a runtime variant that returns the value for the given architecture.
		/// </remarks>
		/// <seealso cref="MinMax_BatchSize" />
		public const int MinMax_Batch = AVXUtils.MinMax_batchSize;

		/// <returns>
		///     The minimum batch size for the <see cref="MinMax" /> function for the current architecture.
		/// </returns>
		[BurstCompile]
		public static int MinMax_BatchSize() {
			if (X86.Avx.IsAvxSupported) return AVXUtils.MinMax_batchSize;
			if (X86.Sse2.IsSse2Supported) return SSE2Utils.MinMax_batchSize;
			return 1;
		}

		/// <summary>
		///     Calculates the minimum and maximum value of the given array.
		///     Array must be a multiple of <see cref="MinMax_Batch" />.
		/// </summary>
		/// <remarks>
		///     Might become obsolete with an update to the Smooth1D noise that would ensure normalised results.
		/// </remarks>
		/// <param name="array">The array to calculate the minimum and maximum from.</param>
		/// <param name="length">The length of the given array.</param>
		/// <param name="minimum">The minimum value result in the array.</param>
		/// <param name="maximum">The maximum value result in the array.</param>
		/// <seealso cref="MinMax_Batch" />
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static unsafe void MinMax([ReadOnly] float* array, int length, out float minimum, out float maximum) {
			if (X86.Avx.IsAvxSupported) AVXUtils.MinMax(array, length, out minimum, out maximum);
			else if (X86.Sse4_1.IsSse41Supported) SSE4Utils.MinMax(array, length, out minimum, out maximum);
			else if (X86.Sse2.IsSse2Supported) SSE2Utils.MinMax(array, length, out minimum, out maximum);
			else MinMax_Default(array, length, out minimum, out maximum);
		}

		/// <inheritdoc cref="MinMax" />
		/// <summary>Default code path for the <see cref="MinMax" /> function.</summary>
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private static unsafe void MinMax_Default([ReadOnly] float* array, int length, out float minimum,
			out float maximum) {
			// Use default code path otherwise.
			minimum = float.MaxValue;
			maximum = float.MinValue;

			for (int i = 0; i < length; i++) {
				float value = array[i];
				minimum = min(minimum, value);
				maximum = max(maximum, value);
			}
		}

		/// <summary>
		///     A jobified version of the <see cref="MinMax" /> function.
		/// </summary>
		public struct MinMaxJob : IJob {
			/// <summary>
			///     The array to calculate the minimum and maximum from.
			/// </summary>
			[ReadOnly] public NativeArray<float> array;

			/// <summary>
			///     The results from the job.
			/// </summary>
			/// <remarks>
			///     This is a two element array:
			///     Element 0 is the minimum,
			///     Element 1 is the maximum.
			/// </remarks>
			[WriteOnly] public NativeArray<float> minMax;

			public void Execute() {
				// Result variables
				float minimum, maximum;
				unsafe {
					// Get pointer from the given array
					float* ptr = (float*)array.GetUnsafeReadOnlyPtr();

					// Pass to function
					MinMax(ptr, array.Length, out minimum, out maximum);
				}

				// Set the results
				minMax[0] = minimum;
				minMax[1] = maximum;
			}
		}

		#endregion
	}
}