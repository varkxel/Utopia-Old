using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia.World.Masks
{
	[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
	public struct MaskSampler : IJobParallelFor
	{
		[ReadOnly]  public NativeArray<float> mask;
		[WriteOnly] public NativeArray<float> chunkMask;
			
		public int2 maskSize;
		public int maskDivisor;
			
		public int2 chunk;
		public int chunkSize;
			
		public void Execute(int index)
		{
			int chunkMaskSize = chunkSize / maskDivisor;

			int2 chunkIndex = chunk * chunkSize;
			int2 indexInChunk = int2(index % chunkSize, index / chunkSize);
			int2 sampleIndex = chunkIndex + indexInChunk;

			float2 maskSample = float2(sampleIndex) / (float) maskDivisor;
			float2 roundedPosition = round(maskSample);

			float2 offset = maskSample - roundedPosition;
			float2 offsetDirection = normalizesafe(offset);
			float offsetMagnitude = length(offset);
				
			int2 baseIndex = (int2) roundedPosition;
			int2 offsetIndex = (int2) round(offsetDirection);
			offsetIndex += baseIndex;
			offsetIndex = clamp(offsetIndex, 0, maskSize);
				
			float baseSample = mask[baseIndex.x + baseIndex.y * chunkMaskSize];
			float offsetSample = mask[offsetIndex.x + offsetIndex.y * chunkMaskSize];
			float value = lerp(baseSample, offsetSample, offsetMagnitude);
			chunkMask[index] = value;
		}
	}
}