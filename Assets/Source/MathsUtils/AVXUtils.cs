using Unity.Burst;
using Unity.Burst.Intrinsics;
using static Unity.Burst.Intrinsics.X86.Sse;
using static Unity.Burst.Intrinsics.X86.Avx;

using Unity.Collections;

using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace MathsUtils
{
	[BurstCompile]
	public static class AVXUtils
	{
		[BurstCompile]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Split(in v256 vec, out v128 lower, out v128 upper)
		{
			lower = mm256_castps256_ps128(vec);
			upper = mm256_extractf128_ps(vec, 1);
		}

		#region Reduction

		/// <summary>Reduces a vector by finding the minimum value.</summary>
		/// <remarks>Superseded by <see cref="math.cmin(int2)"/>.</remarks>
		/// <param name="vec">The vector to reduce.</param>
		/// <returns>The minimum value in the vector.</returns>
		[BurstCompile]
		public static float ReduceMin(in v256 vec)
		{
			Split(vec, out v128 lower, out v128 upper);
			lower = min_ps(lower, upper);
			return SSE4Utils.ReduceMin(lower);
		}

		/// <summary>Reduces a vector by finding the maximum value.</summary>
		/// <remarks>Superseded by <see cref="math.cmax(int2)"/>.</remarks>
		/// <param name="vec">The vector to reduce.</param>
		/// <returns>The maximum value in the vector.</returns>
		[BurstCompile]
		public static float ReduceMax(in v256 vec)
		{
			Split(vec, out v128 lower, out v128 upper);
			lower = max_ps(lower, upper);
			return SSE4Utils.ReduceMax(lower);
		}

		#endregion

		/// <summary>
		/// The size of a batch for the <see cref="MinMax"/> function.
		/// </summary>
		public const int MinMax_batchSize = 8;

		/// <inheritdoc cref="MathsUtil.MinMax"/>
		/// <summary>
		/// AVX Pathway for the <see cref="MathsUtil.MinMax"/> function.
		/// I found that the burst compiler isn't generating code utilising the YMM registers,
		/// so have developed a pathway specifically for AVX-capable chips that does utilise the 256-bit registers.
		/// </summary>
		/// <remarks>
		/// AVX512 improvements are possible beyond the 512-bit registers:
		/// The reduce instruction would be extremely useful for coalescing the final values into a float.
		/// </remarks>
		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		public static unsafe void MinMax([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			// Safety check since Unity requires it.
			if(!IsAvxSupported)
			{
				minimum = float.MaxValue;
				maximum = float.MinValue;
				return;
			}

			// Initialise the outputs
			minimum = float.MaxValue;
			maximum = float.MaxValue;

			// Calculate the chunks to operate on, and leftovers
			int remainder = length % MinMax_batchSize;
			int lengthFloor = length - remainder;

			// Create registers
			v256 minRegister = new v256(float.MaxValue);
			v256 maxRegister = new v256(float.MinValue);

			// Loop through the array
			for(int offset = 0; offset < lengthFloor; offset += MinMax_batchSize)
			{
				// Store 8 floats from the array
				v256 valRegister = mm256_load_ps(&array[offset]);

				// Calculate the min/max for the registers
				minRegister = mm256_min_ps(minRegister, valRegister);
				maxRegister = mm256_max_ps(maxRegister, valRegister);
			}

			// Reduce vectors into single values
			minimum = ReduceMin(minRegister);
			maximum = ReduceMax(maxRegister);
		}
	}
}