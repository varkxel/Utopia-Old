using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia
{
	[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
	public struct Curve : System.IDisposable
	{
		public int length;
		[ReadOnly] public NativeArray<float> x;
		[ReadOnly] public NativeArray<float> y;
		[ReadOnly] public NativeArray<float> tangentIn;
		[ReadOnly] public NativeArray<float> tangentOut;

		public unsafe struct RawData
		{
			public int length;
			[ReadOnly] public float* xPtr;
			[ReadOnly] public float* yPtr;
			[ReadOnly] public float* tInPtr;
			[ReadOnly] public float* tOutPtr;
		}

		public unsafe RawData GetRawData() => new RawData()
		{
			length = this.length,
			xPtr = (float*) x.GetUnsafeReadOnlyPtr(),
			yPtr = (float*) y.GetUnsafeReadOnlyPtr(),
			tInPtr = (float*) tangentIn.GetUnsafeReadOnlyPtr(),
			tOutPtr = (float*) tangentOut.GetUnsafeReadOnlyPtr()
		};

		public Curve(AnimationCurve curve, Allocator allocator)
		{
			length = curve.length;
			x = new NativeArray<float>(length, allocator, NativeArrayOptions.UninitializedMemory);
			y = new NativeArray<float>(length, allocator, NativeArrayOptions.UninitializedMemory);
			tangentIn = new NativeArray<float>(length, allocator, NativeArrayOptions.UninitializedMemory);
			tangentOut = new NativeArray<float>(length, allocator, NativeArrayOptions.UninitializedMemory);

			for(int i = 0; i < length; i++)
			{
				Keyframe key = curve.keys[i];
				x[i] = key.time;
				y[i] = key.value;
				tangentIn[i] = key.inTangent;
				tangentOut[i] = key.outTangent;
			}
		}

		public void Dispose()
		{
			x.Dispose();
			y.Dispose();
			tangentIn.Dispose();
			tangentOut.Dispose();
		}

		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		public static unsafe float Evaluate(float point, [ReadOnly] in RawData data)
		{
			if(point <= data.xPtr[0]) return data.yPtr[0];

			int lastElement = data.length - 1;
			if(point >= data.xPtr[lastElement]) return data.yPtr[lastElement - 1];

			int leftSample = 0;
			int rightSample = 0;
			for(int i = 0; i < data.length - 1; i++)
			{
				if(data.xPtr[i] <= point)
				{
					leftSample = i;
					rightSample = i + 1;
				}
			}

			return EvaluateInterval
			(
				data.xPtr[leftSample], data.xPtr[rightSample], point,
				data.yPtr[leftSample], data.yPtr[rightSample],
				data.tOutPtr[leftSample], data.tInPtr[rightSample]
			);
		}

		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		private static float EvaluateInterval
		(
			// X Val
			float xLeft, float xRight, float xInterpolation,
			// Y Val
			float yLeft, float yRight,
			// Tangent
			float tLeft, float tRight
		)
		{
			/*
				Designed on this curve implementation by 5argon:
				https://github.com/5argon/JobAnimationCurve/blob/master/JobAnimationCurve.cs
			*/

			float interpolation = unlerp(xLeft, xRight, xInterpolation);
			float xDifference = xRight - xLeft;

			float4 parameters = new float4
			(
				interpolation * interpolation * interpolation,
				interpolation * interpolation,
				interpolation,
				1.0f
			);
			float4x4 hermite = new float4x4
			(
				2, -2, 1, 1,
				-3, 3, -2, -1,
				0, 0, 1, 0,
				1, 0, 0, 0
			);

			float4 control = new float4(yLeft, yRight, tLeft, tRight) * new float4(1, 1, xDifference, xDifference);
			float4 hermiteResult = mul(parameters, hermite);
			float4 hermiteBlend = control * hermiteResult;
			return csum(hermiteBlend);
		}
	}
}