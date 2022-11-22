using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Utopia.Noise;

namespace Utopia.World.Masks
{
	[BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
	public struct ExtentsJob : IJobParallelFor
	{
		public float seed;
		public float scale;
		public uint octaves;
		public float lacunarity;
		public float gain;

		[ReadOnly]  public NativeArray<float> angles;
		[WriteOnly] public NativeArray<float> extents;

		public void Execute(int index)
		{
			float samplePoint = seed;
			samplePoint += angles[index] * scale;

			float extent = Smooth1D.Fractal(samplePoint, octaves, lacunarity, gain);
			extents[index] = extent;
		}
	}
}