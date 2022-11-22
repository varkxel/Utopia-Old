using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;

namespace Utopia.World.Masks
{
	[BurstCompile]
	public struct SmoothJob : IJobParallelFor
	{
		[WriteOnly] public NativeSlice<float> array;

		public float startSample;
		public float endSample;

		public int smoothSamples;

		public void Execute(int i)
		{
			float position = (float) i / (float) smoothSamples;
			array[i] = lerp(startSample, endSample, smoothstep(0, 1, position));
		}
	}
}