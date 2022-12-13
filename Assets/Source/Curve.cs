using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia
{
	[BurstCompile]
	public struct Curve : System.IDisposable
	{
		public int length;
		public NativeArray<float> x;
		public NativeArray<float> y;
		public NativeArray<float> tangentIn;
		public NativeArray<float> tangentOut;

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
		public static unsafe float Evaluate
		(
			float point, int length,
			[ReadOnly] float* x, [ReadOnly] float* y,
			[ReadOnly] float* tangentIn, [ReadOnly] float* tangentOut
		)
		{
			if(point <= x[0]) return y[0];

			int lastElement = length - 1;
			if(point >= x[lastElement]) return y[lastElement - 1];

			int leftSample = 0;
			int rightSample = 0;
			for(int i = 0; i < length - 1; i++)
			{
				if(x[i] <= point)
				{
					leftSample = i;
					rightSample = i + 1;
				}
			}

			return EvaluateInterval
			(
				x[leftSample], x[rightSample], point,
				y[leftSample], y[rightSample],
				tangentOut[leftSample], tangentIn[rightSample]
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