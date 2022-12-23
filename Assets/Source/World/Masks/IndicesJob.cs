using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Utopia.World.Masks
{
	/// <summary>
	/// Generates the indices for the mask's mesh.
	/// </summary>
	[BurstCompile]
	public struct IndicesJob : IJob
	{
		// Results
		public NativeArray<int> indices;

		public void Execute()
		{
			int currentIndex = 1;

			int indicesCount = indices.Length;
			for(int i = 0; i < indicesCount; i++)
			{
				// Make every first index equal to zero.
				int multiplier = (i % 3 != 0) ? 1 : 0;
				indices[i] = currentIndex * multiplier;

				// Increment the counter only on each 2nd index.
				currentIndex += (i % 3 == 1) ? 1 : 0;
			}

			// Final triangle
			indices[indicesCount - 3] = 0;
			indices[indicesCount - 2] = indices[indicesCount - 4];
			indices[indicesCount - 1] = indices[1];
		}
	}
}