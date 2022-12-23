using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;

namespace Utopia.World.Masks
{
	/// <summary>
	/// Parallel job to smooth the final part of the mask mesh vertices,
	/// where the angles loop back around.
	/// </summary>
	[BurstCompile]
	public struct SmoothJob : IJobParallelFor
	{
		// Input samples
		public float startSample;
		public float endSample;

		// Output slice
		[WriteOnly] public NativeSlice<float> array;

		/// <summary>
		/// The amount of samples to replace.
		/// </summary>
		public int smoothSamples;

		public void Execute(int i)
		{
			float position = (float) i / (float) smoothSamples;
			array[i] = lerp(startSample, endSample, smoothstep(0, 1, position));
		}
	}
}