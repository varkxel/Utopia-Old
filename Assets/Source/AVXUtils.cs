using Unity.Burst;
using Unity.Burst.Intrinsics;
using static Unity.Burst.Intrinsics.X86.Sse;
using static Unity.Burst.Intrinsics.X86.Avx;

using Unity.Collections;

using System.Runtime.CompilerServices;

namespace Utopia
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

		[BurstCompile]
		public static float ReduceMin(in v256 vec)
		{
			Split(vec, out v128 lower, out v128 upper);
			lower = min_ps(lower, upper);
			return SSE4Utils.ReduceMin(lower);
		}

		[BurstCompile]
		public static float ReduceMax(in v256 vec)
		{
			Split(vec, out v128 lower, out v128 upper);
			lower = max_ps(lower, upper);
			return SSE4Utils.ReduceMax(lower);
		}

		#endregion

		public const int MinMax_batchSize = 8;

		/// <summary>
		/// AVX Pathway for the MinMax function.
		/// I found that the burst compiler isn't generating code utilising the YMM registers,
		/// so have developed a pathway specifically for AVX-capable chips that does utilise the 256-bit registers.
		/// </summary>
		/// <remarks>
		/// AVX512 improvements are possible beyond the 512-bit registers:
		/// The reduce instruction would be extremely useful for coalescing the final values into a float.
		/// </remarks>
		/// <param name="array">Read-only pointer to the array to calculate min/max from.</param>
		/// <param name="length">Length of the array to operate on.</param>
		/// <param name="minimum">The minimum value in the array.</param>
		/// <param name="maximum">The maximum value in the array.</param>
		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		public static unsafe void MinMax([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			// Initialise the outputs
			minimum = float.MaxValue;
			maximum = float.MaxValue;

			// Calculate the chunks to operate on, and leftovers
			int remainder = length % MinMax_batchSize;
			int lengthFloor = length - remainder;

			// Create registers
			v256 minRegister = new v256(float.MaxValue);
			v256 maxRegister = new v256(float.MinValue);

			// Loop through array
			for(int offset = 0; offset < lengthFloor; offset += MinMax_batchSize)
			{
				// Store 8 floats from the array
				v256 valRegister = mm256_load_ps(&array[offset]);

				// Calculate the min/max for the registers
				minRegister = mm256_min_ps(minRegister, valRegister);
				maxRegister = mm256_max_ps(maxRegister, valRegister);
			}

			minimum = ReduceMin(minRegister);
			maximum = ReduceMax(maxRegister);
		}
	}
}