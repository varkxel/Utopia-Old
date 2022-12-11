using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia
{
	/*
	 * Designed on this curve implementation by 5argon:
	 * https://github.com/5argon/JobAnimationCurve/blob/master/JobAnimationCurve.cs
	 */
	[System.Serializable, BurstCompile]
	public struct Curve
	{
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