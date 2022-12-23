using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Utopia.Noise;

namespace Utopia.World.Masks
{
	/// <summary>
	/// Parallel job to generate the extents for the mask mesh,
	/// given the input angles from the <see cref="AnglesJob"/>.
	/// </summary>
	[BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
	public struct ExtentsJob : IJobParallelFor
	{
		// 1D noise properties
		public float seed;
		public float scale;
		public uint octaves;
		public float lacunarity;
		public float gain;

		// Input angles
		[ReadOnly]  public NativeArray<float> angles;

		// Results
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