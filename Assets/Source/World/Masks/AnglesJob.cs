using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia.World.Masks
{
	[BurstCompile(FloatPrecision.Medium, FloatMode.Default)]
	public struct AnglesJob : IJob
	{
		public Random random;

		public int angleCount;
		[WriteOnly] public NativeArray<float> angles;

		public void Execute()
		{
			const float start = -PI + EPSILON;
			float baseOffset = (2.0f * PI) / (float) angleCount;

			float currentAngle = start;
			float lastOffset = 0.0f;

			for(int i = 0; i < angleCount; i++)
			{
				angles[i] = currentAngle;

				float offset = random.NextFloat(0.0f, baseOffset);
				currentAngle += (baseOffset - lastOffset) + offset;

				lastOffset = offset;
			}
		}
	}
}