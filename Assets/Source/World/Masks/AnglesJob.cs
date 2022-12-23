using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia.World.Masks {
	/// <summary>
	///     Generates a set of somewhat equally spaced angles
	///     to use as directions for the extents on the mask mesh.
	/// </summary>
	[BurstCompile(FloatPrecision.Medium, FloatMode.Default)]
	public struct AnglesJob : IJob {
		// Random instance to use
		public Random random;

		// Amount of angles to generate
		public int angleCount;

		// Results
		[WriteOnly] public NativeArray<float> angles;

		public void Execute() {
			// Calculate starting position and offset
			const float start = -PI + EPSILON;
			float baseOffset = 2.0f * PI / angleCount;

			float currentAngle = start;
			float lastOffset = 0.0f;

			for (int i = 0; i < angleCount; i++) {
				// Add the angle
				angles[i] = currentAngle;

				// Offset by a random amount to give variance to the sample positions.
				float offset = random.NextFloat(0.0f, baseOffset);
				currentAngle += baseOffset - lastOffset + offset;

				lastOffset = offset;
			}
		}
	}
}