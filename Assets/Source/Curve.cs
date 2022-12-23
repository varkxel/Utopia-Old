using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Utopia {
	/// <summary>
	///     A DOTS-Compatible <see cref="AnimationCurve" /> implementation.
	/// </summary>
	[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
	public struct Curve : IDisposable {
		/// <summary>
		///     The amount of samples in the curve / the amount of elements in the arrays.
		/// </summary>
		public readonly int length;

		// Curve data
		[ReadOnly] public NativeArray<float> x;
		[ReadOnly] public NativeArray<float> y;
		[ReadOnly] public NativeArray<float> tangentIn;
		[ReadOnly] public NativeArray<float> tangentOut;

		/// <summary>
		///     The raw data access for <see cref="Curve.Evaluate" /> to use.
		///     Use <see cref="Curve.GetRawData" /> to generate the struct.
		/// </summary>
		public unsafe struct RawData {
			public int length;

			[ReadOnly] public float* xPtr;
			[ReadOnly] public float* yPtr;
			[ReadOnly] public float* tInPtr;
			[ReadOnly] public float* tOutPtr;
		}

		/// <summary>
		///     Creates a <see cref="RawData" /> object to be used in jobs with <see cref="Evaluate" />.
		/// </summary>
		public unsafe RawData GetRawData() {
			return new RawData {
				length = length,
				xPtr = (float*)x.GetUnsafeReadOnlyPtr(),
				yPtr = (float*)y.GetUnsafeReadOnlyPtr(),
				tInPtr = (float*)tangentIn.GetUnsafeReadOnlyPtr(),
				tOutPtr = (float*)tangentOut.GetUnsafeReadOnlyPtr()
			};
		}

		/// <summary>
		///     Converts the given AnimationCurve into a <see cref="Curve" />.
		/// </summary>
		/// <param name="curve">The animation curve to convert.</param>
		/// <param name="allocator">What the created <see cref="Curve" /> should be allocated as.</param>
		public Curve(AnimationCurve curve, Allocator allocator) {
			length = curve.length;
			x = new NativeArray<float>(length, allocator, NativeArrayOptions.UninitializedMemory);
			y = new NativeArray<float>(length, allocator, NativeArrayOptions.UninitializedMemory);
			tangentIn = new NativeArray<float>(length, allocator, NativeArrayOptions.UninitializedMemory);
			tangentOut = new NativeArray<float>(length, allocator, NativeArrayOptions.UninitializedMemory);

			for (int i = 0; i < length; i++) {
				Keyframe key = curve.keys[i];
				x[i] = key.time;
				y[i] = key.value;
				tangentIn[i] = key.inTangent;
				tangentOut[i] = key.outTangent;
			}
		}

		public void Dispose() {
			x.Dispose();
			y.Dispose();
			tangentIn.Dispose();
			tangentOut.Dispose();
		}

		/// <summary>
		///     Evaluates the given curve data.
		/// </summary>
		/// <param name="point">The X value of the curve.</param>
		/// <param name="data">The data of the curve, generated from <see cref="GetRawData" />.</param>
		/// <returns>The Y value of the curve.</returns>
		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		public static unsafe float Evaluate(float point, [ReadOnly] in RawData data) {
			// If the X value is below the lowest value, return the lowest value as a clamp.
			if (point <= data.xPtr[0]) return data.yPtr[0];

			int lastElement = data.length - 1;

			// If the X value is above the highest value, return the highest value as a clamp.
			if (point >= data.xPtr[lastElement]) return data.yPtr[lastElement - 1];

			// Find the two samples that the given X value lies between.
			int leftSample = 0;
			int rightSample = 0;
			for (int i = 0; i < data.length - 1; i++)
				if (data.xPtr[i] <= point) {
					leftSample = i;
					rightSample = i + 1;
				}

			// Evaluate between the two samples with the cubic hermite spline function.
			return EvaluateInterval
			(
				data.xPtr[leftSample], data.xPtr[rightSample], point,
				data.yPtr[leftSample], data.yPtr[rightSample],
				data.tOutPtr[leftSample], data.tInPtr[rightSample]
			);
		}

		/// <summary>
		///     Evaluates a cubic hermite spline between the two given values.
		/// </summary>
		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		private static float EvaluateInterval
		(
			// X Val
			float xLeft, float xRight, float xPoint,
			// Y Val
			float yLeft, float yRight,
			// Tangent
			float tLeft, float tRight
		) {
			// Calculate the weighting between the left and right sample
			float xInterpolation = unlerp(xLeft, xRight, xPoint);

			// Calculate the difference between the samples
			float xDifference = xRight - xLeft;

			/*
				Designed on this curve implementation by 5argon:
				https://github.com/5argon/JobAnimationCurve/blob/master/JobAnimationCurve.cs
			*/
			float4 parameters = new float4
			(
				xInterpolation * xInterpolation * xInterpolation,
				xInterpolation * xInterpolation,
				xInterpolation,
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