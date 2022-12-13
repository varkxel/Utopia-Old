using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static Unity.Mathematics.math;

using Stella3D;

namespace Utopia
{
	[BurstCompile, System.Serializable]
	public class Curve : System.IDisposable
	{
		public float[] _x;
		public float[] _y;
		public float[] _tangentIn;
		public float[] _tangentOut;

		public SharedArray<float> x;
		public SharedArray<float> y;
		public SharedArray<float> tangentIn;
		public SharedArray<float> tangentOut;

		public unsafe struct RawData
		{
			public int length;
			public float* x;
			public float* y;
			public float* tangentIn;
			public float* tangentOut;
		}

		public unsafe RawData GetRawData()
		{
			NativeArray<float> xNative = x;
			NativeArray<float> yNative = y;
			NativeArray<float> tInNative = tangentIn;
			NativeArray<float> tOutNative = tangentOut;

			return new RawData()
			{
				length = x.Length,
				x = (float*) xNative.GetUnsafeReadOnlyPtr(),
				y = (float*) yNative.GetUnsafeReadOnlyPtr(),
				tangentIn = (float*) tInNative.GetUnsafeReadOnlyPtr(),
				tangentOut = (float*) tOutNative.GetUnsafeReadOnlyPtr()
			};
		}

		public void Initialise()
		{
			x = new SharedArray<float>(_x);
			y = new SharedArray<float>(_y);
			tangentIn = new SharedArray<float>(_tangentIn);
			tangentOut = new SharedArray<float>(_tangentOut);
		}

		public void Dispose()
		{
			x.Dispose();
			y.Dispose();
			tangentIn.Dispose();
			tangentOut.Dispose();
		}

		public float Evaluate(float point)
		{
			return Evaluate(point, GetRawData());
		}

		[BurstCompile]
		public static unsafe float Evaluate(float point, in RawData data)
		{
			if(point <= data.x[0]) return data.y[0];

			int lastElement = data.length - 1;
			if(point >= data.x[lastElement]) return data.y[lastElement - 1];

			int leftSample = 0;
			int rightSample = 0;
			for(int i = 1; i < lastElement; i++)
			{
				if(point <= data.x[i]) break;
				
				leftSample = i - 1;
				rightSample = i;
			}

			return EvaluateInterval
			(
				data.x[leftSample], data.x[rightSample], point,
				data.y[leftSample], data.y[rightSample],
				data.tangentOut[leftSample], data.tangentIn[rightSample]
			);
		}

		[BurstCompile]
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