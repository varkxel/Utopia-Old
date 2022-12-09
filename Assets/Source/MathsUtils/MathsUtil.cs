using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace MathsUtils
{
	[BurstCompile]
	public static class MathsUtil
	{
		public static void ToVec(in float4 vector, out v128 register)
		{
			if(!X86.Sse.IsSseSupported)
			{
				// Probably won't get called, probably won't work either.
				register = new v128(vector.x, vector.y, vector.z, vector.w);
				return;
			}
			unsafe
			{
				// Generate vector, should get condensed.
				float* vecData = stackalloc float[4];
				for(int i = 0; i < 4; i++) vecData[i] = vector[i];
				register = X86.Sse.load_ps(vecData);
			}
		}
		
		#region MinMax
		
		public const int MinMax_MaxBatch = AVXUtils.MinMax_batchSize;

		[BurstCompile]
		public static int MinMax_BatchSize()
		{
			if(X86.Avx.IsAvxSupported) return AVXUtils.MinMax_batchSize;
			else if(X86.Sse2.IsSse2Supported) return SSE2Utils.MinMax_batchSize;
			else return 1;
		}

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		public static unsafe void MinMax([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			if(X86.Avx.IsAvxSupported)            AVXUtils.MinMax(array, length, out minimum, out maximum);
			else if(X86.Sse4_1.IsSse41Supported) SSE4Utils.MinMax(array, length, out minimum, out maximum);
			else if(X86.Sse2.IsSse2Supported)    SSE2Utils.MinMax(array, length, out minimum, out maximum);
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

		public struct MinMaxJob : IJob
		{
			[ReadOnly] public NativeArray<float> array;

			[WriteOnly] public NativeArray<float> minMax;

			public void Execute()
			{
				float minimum, maximum;
				unsafe
				{
					float* ptr = (float*) array.GetUnsafeReadOnlyPtr();
					MinMax(ptr, array.Length, out minimum, out maximum);
				}
				minMax[0] = minimum;
				minMax[1] = maximum;
			}
		}
		
		#endregion
		
		[BurstCompile]
		public static float MinItem(in float4 items)
		{
			if(X86.Sse2.IsSse2Supported)
			{
				ToVec(items, out v128 vec);
				return X86.Sse4_1.IsSse41Supported ? SSE4Utils.ReduceMin(vec) : SSE2Utils.ReduceMin(vec);
			}
			else
			{
				float minimum = float.MaxValue;
				for(int i = 0; i < 4; i++) minimum = min(minimum, items[i]);
				return minimum;
			}
		}
		
		[BurstCompile]
		public static float MaxItem(in float4 items)
		{
			if(X86.Sse2.IsSse2Supported)
			{
				ToVec(items, out v128 vec);
				return X86.Sse4_1.IsSse41Supported ? SSE4Utils.ReduceMax(vec) : SSE2Utils.ReduceMax(vec);
			}
			else
			{
				float maximum = float.MaxValue;
				for(int i = 0; i < 4; i++) maximum = max(maximum, items[i]);
				return maximum;
			}
		}
	}
}