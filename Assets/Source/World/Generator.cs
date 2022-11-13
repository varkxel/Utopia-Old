using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine.Rendering;
using float2 = Unity.Mathematics.float2;
using Random = Unity.Mathematics.Random;

namespace Utopia.World
{
	[BurstCompile]
	public class Generator : MonoBehaviour
	{
		[NonSerialized] public Random random;
		
		[Header("World")]
		public float worldSize = 16384;

		[Header("Mask")]
		public Mask mask = new Mask();
		public float maskDivisor = 4;
		
		public bool isMaskGenerated { get; private set; } = false;
		private NativeArray<float> maskData;
		private int maskSize;
		
		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe float SampleMaskRaw([ReadOnly] float* array, in int2 index, int maskSize)
		{
			return array[index.x + index.y * maskSize];
		}
		
		[BurstCompile]
		public static unsafe float SampleMask([ReadOnly] float* array, in float2 position, float maskDivisor, int maskSize)
		{
			float2 scaledPosition = position / maskDivisor;
			float2 roundedPosition = round(scaledPosition);

			float2 positionOffset = scaledPosition - roundedPosition;
			float2 offsetDirection = normalizesafe(positionOffset);
			
			int2 offsetIndex = (int2) round(offsetDirection);
			int2 baseIndex = (int2) roundedPosition;

			int2 offsetIndexX = baseIndex + new int2(offsetIndex.x, 0);
			int2 offsetIndexY = baseIndex + new int2(0, offsetIndex.y);

			float2 offsetSample = new float2
			(
				SampleMaskRaw(array, offsetIndexX, maskSize),
				SampleMaskRaw(array, offsetIndexY, maskSize)
			);
			float baseSample = SampleMaskRaw(array, baseIndex, maskSize);

			float2 interpolated = lerp(baseSample, offsetSample, positionOffset);
			float value = (interpolated.x + interpolated.y) / 2.0f;
			return value;
		}

		void OnValidate()
		{
			float divisor = max(maskDivisor, Mask.batchSize);
			worldSize /= divisor;
			worldSize = round(worldSize);
			worldSize *= divisor;
		}
		
		void Start()
		{
			GenerateMask();
		}

		void OnDestroy()
		{
			ResetMask();
		}

		public void ResetMask()
		{
			if(isMaskGenerated)
			{
				maskData.Dispose();
				isMaskGenerated = false;
			}
		}

		public void GenerateMask()
		{
			ResetMask();

			maskSize = (int) worldSize / (int) maskDivisor;
			Mask mask = new Mask(maskSize);
			mask.Generate(ref random);
			maskData = new NativeArray<float>(maskSize * maskSize, Allocator.Persistent);
			AsyncGPUReadback.RequestIntoNativeArray(ref maskData, mask.gpuResult, 0, request =>
			{
				if(request.hasError)
				{
					Debug.LogError("Error requesting island mask data from GPU.");
					return;
				}
				
				isMaskGenerated = true;
				mask.gpuResult.DiscardContents();
			});
		}
	}
}