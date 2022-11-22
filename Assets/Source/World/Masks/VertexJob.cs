using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia.World.Masks
{
	[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
	public struct VertexJob : IJobParallelFor
	{
		[ReadOnly]  public NativeArray<float> angles;

		[ReadOnly]  public NativeArray<float> extents;
		[ReadOnly]  public NativeArray<float> extentsMinMax;

		[WriteOnly] public NativeSlice<float3> vertices;

		public void Execute(int index)
		{
			float angle = angles[index];
			float2 direction = float2(cos(angle), sin(angle));

			float extent = extents[index];
			extent = unlerp(extentsMinMax[0], extentsMinMax[1], extent);
			direction *= extent;

			vertices[index] = float3(direction, 0.0f);
		}
	}
}