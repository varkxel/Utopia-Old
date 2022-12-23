using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia.World.Masks {
	/// <summary>
	///     Calculates the vertex positions from the given angles and extents.
	/// </summary>
	[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
	public struct VertexJob : IJobParallelFor {
		// Input angles & extents
		[ReadOnly] public NativeArray<float> angles;
		[ReadOnly] public NativeArray<float> extents;

		// Normalisation bounds:
		//   0 = Min
		//   1 = Max
		[ReadOnly] public NativeArray<float> extentsMinMax;

		// Results array
		[WriteOnly] public NativeSlice<float3> vertices;

		public void Execute(int index) {
			// Calculate the direction vector from the angle
			float angle = angles[index];
			float2 direction = float2(cos(angle), sin(angle));

			// Get extent
			float extent = extents[index];

			// Normalise the extent
			extent = unlerp(extentsMinMax[0], extentsMinMax[1], extent);

			// Calculate vertex position
			direction *= extent;

			// Transpose to 3D
			vertices[index] = float3(direction, 0.0f);
		}
	}
}