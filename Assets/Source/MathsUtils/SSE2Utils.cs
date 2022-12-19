using Unity.Burst;
using Unity.Burst.Intrinsics;
using static Unity.Burst.Intrinsics.X86.Sse;
using static Unity.Burst.Intrinsics.X86.Sse2;

using Unity.Collections;

using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace MathsUtils
{
	[BurstCompile]
	public static class SSE2Utils
	{
		#region Reduction

		/*
			Designed around this answer:
			https://stackoverflow.com/a/35270026
		*/
		
		[BurstCompile]
		public static float ReduceMin(in v128 vec)
		{
			v128 shuffled = shuffle_ps(vec, vec, SHUFFLE(2, 3, 0, 1));
			v128 min = min_ps(vec, shuffled);
			shuffled = movehl_ps(shuffled, min);
			min = min_ss(min, shuffled);
			return cvtss_f32(min);
		}
		
		[BurstCompile]
		public static float ReduceMax(in v128 vec)
		{
			v128 shuffled = shuffle_ps(vec, vec, SHUFFLE(2, 3, 0, 1));
			v128 max = max_ps(vec, shuffled);
			shuffled = movehl_ps(shuffled, max);
			max = max_ss(max, shuffled);
			return cvtss_f32(max);
		}

		#endregion

		/// <summary>
		/// The size of a batch for the <see cref="MinMax"/> function.
		/// </summary>
		public const int MinMax_batchSize = 4;

		[BurstCompile]
		internal static unsafe void MinMax([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			MinMax_SSE2Base(array, length, out v128 minRegister, out v128 maxRegister);

			// Reduce vectors into single values
			minimum = ReduceMin(minRegister);
			maximum = ReduceMax(maxRegister);
		}

		[BurstCompile]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe void MinMax_SSE2Base([ReadOnly] float* array, int length, out v128 minRegister, out v128 maxRegister)
		{
			// Calculate the chunks to operate on, and leftovers
			int remainder = length % MinMax_batchSize;
			int lengthFloor = length - remainder;

			// Create registers
			minRegister = new v128(float.MaxValue);
			maxRegister = new v128(float.MinValue);

			// Loop through the array
			for(int offset = 0; offset < lengthFloor; offset += MinMax_batchSize)
			{
				// Store 4 floats from the array
				v128 valRegister = load_ps(&array[offset]);

				// Calculate the min/max for the registers
				minRegister = min_ps(minRegister, valRegister);
				maxRegister = max_ps(maxRegister, valRegister);
			}
		}

		[BurstCompile]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void MakeUnique(in v128 vec, out v128 result)
		{
			result = vec;

			// SSE2 Safety Check
			if(!IsSse2Supported) return;

			// Create xor mask to flip the bool vector
			v128 mask = new v128(1u, 1u, 1u, 1u);

			v128 shifted = vec;
			for(uint i = 0; i < 3; i++)
			{
				// Shift vector values right to next element
				shifted = srli_si128(shifted, 4);

				// Flip the shifted vector
				v128 flipped = xor_si128(shifted, mask);

				// Compare against result
				result = and_si128(result, flipped);
			}
		}
	}
}