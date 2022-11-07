using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Utopia.Noise;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;
using static Unity.Burst.Intrinsics.X86.Sse;
using static Unity.Burst.Intrinsics.X86.Sse2;
using static Unity.Burst.Intrinsics.X86.Sse4_1;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace Utopia.World
{
	[BurstCompile]
	public class Mask
	{
		[System.Serializable]
		public struct GenerationSettings
		{
			[Header("Mesh Settings")]
			[Range(4, 65535)] public int complexity;

			[Header("Noise Generation")]
			public float scale;
			
			public uint octaves;
			public float gain;
			public float lacunarity;
		}
		public GenerationSettings settings;
		
		public RenderTexture result { get; private set; }

		private CommandBuffer commandBuffer = new CommandBuffer()
		{
			name = "MaskGenerator"
		};

		private static readonly float4x4 matrixTransform = float4x4.TRS(float3(0), quaternion.identity, float3(0.5f));
		private static readonly float4x4 matrixLook = float4x4.TRS(float3(0, 0, -1), quaternion.identity, float3(1));
		private static readonly float4x4 matrixOrtho = float4x4.Ortho(2, 2, 0.01f, 2);

		private const string shader = "Hidden/Utopia/World/MaskGenerator";
		private Material material;

		public Mask(int size, GenerationSettings settings)
		{
			this.settings = settings;

			material = new Material(Shader.Find(shader));
			result = new RenderTexture(size, size, 0, RenderTextureFormat.RFloat)
			{
				filterMode = FilterMode.Trilinear
			};
		}

		public void Generate(ref Random random)
		{
			int verticesCount = settings.complexity;
			verticesCount -= verticesCount % 4;

			AnglesJob anglesJob = new AnglesJob()
			{
				random = random,
				angles = new NativeArray<float>(verticesCount, Allocator.TempJob),
				angleCount = verticesCount
			};
			JobHandle anglesHandle = anglesJob.Schedule();
			
			// Schedule indices job whilst angles job runs
			IndicesJob indicesJob = new IndicesJob()
			{
				indices = new NativeArray<int>(((verticesCount - 2) * 3) + 3, Allocator.TempJob)
			};
			JobHandle indicesJobHandle = indicesJob.Schedule();
			
			anglesHandle.Complete();
			NativeArray<float> angles = anglesJob.angles;
			random = anglesJob.random;

			ExtentsJob extentsJob = new ExtentsJob()
			{
				extents = new NativeArray<float>(verticesCount, Allocator.TempJob),
				angles = new NativeArray<float>(verticesCount, Allocator.TempJob),

				seed = random.NextFloat(),
				settings = settings
			};
			angles.CopyTo(extentsJob.angles);
			JobHandle extentsHandle = extentsJob.Schedule(verticesCount, 4);
			JobHandle anglesSortJob = angles.SortJob().Schedule();
			
			NativeArray<float3> vertices = new NativeArray<float3>(verticesCount + 1, Allocator.TempJob);
			vertices[0] = float3.zero;
			
			extentsHandle.Complete();
			extentsJob.angles.Dispose();
			NativeArray<float> extents = extentsJob.extents;
			
			NativeArray<float> extentsClone = new NativeArray<float>(extents, Allocator.Temp);
			JobHandle extentsSortJob = extents.SortJob().Schedule();
			
			// Calculate the min/max whilst the extents are sorted, using a duplicated array.
			float extentsMin, extentsMax;
			unsafe
			{
				MinMax((float*) extentsClone.GetUnsafeReadOnlyPtr(), extents.Length, out extentsMin, out extentsMax);
			}
			extentsClone.Dispose();
			
			// Sort the extents along with the angles
			extentsSortJob.Complete();
			anglesSortJob.Complete();
			
			// Normalisation done in vertex job
			VertexJob vertexJob = new VertexJob()
			{
				angles = angles,
				vertices = vertices.Slice(1),
				extents = extents,
				
				extentsMin = extentsMin,
				extentsMax = extentsMax
			};
			JobHandle vertexJobHandle = vertexJob.Schedule(verticesCount, 4);
			
			commandBuffer.SetRenderTarget(result);
			commandBuffer.ClearRenderTarget(false, true, Color.black);
			commandBuffer.SetViewProjectionMatrices(matrixLook, matrixOrtho);
			
			vertexJobHandle.Complete();
			
			angles.Dispose();
			
			indicesJobHandle.Complete();

			int indicesLength = indicesJob.indices.Length;
			indicesJob.indices[indicesLength - 3] = 0;
			indicesJob.indices[indicesLength - 2] = indicesJob.indices[indicesLength - 4];
			indicesJob.indices[indicesLength - 1] = indicesJob.indices[1];

			Mesh mesh = new Mesh()
			{
				vertices = vertices.Reinterpret<Vector3>().ToArray(),
				triangles = indicesJob.indices.ToArray(),

				indexFormat = IndexFormat.UInt16
			};
			commandBuffer.DrawMesh(mesh, matrixTransform, material);
			Graphics.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Clear();

			vertices.Dispose();
			indicesJob.indices.Dispose();
		}

		[BurstCompile(FloatPrecision.High, FloatMode.Default)]
		private struct AnglesJob : IJob
		{
			public Random random;

			public int angleCount;
			[WriteOnly] public NativeArray<float> angles;

			public void Execute()
			{
				for(int i = 0; i < angleCount; i++)
				{
					angles[i] = random.NextFloat(-PI + EPSILON, PI);
				}
			}
		}

		[BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
		private struct ExtentsJob : IJobParallelFor
		{
			public float seed;
			public GenerationSettings settings;

			[ReadOnly]  public NativeArray<float> angles;
			[WriteOnly] public NativeArray<float> extents;

			public void Execute(int index)
			{
				float samplePoint = seed;
				samplePoint += angles[index] * settings.scale;

				float extent = Smooth1D.Fractal(samplePoint, settings.octaves, settings.lacunarity, settings.gain);
				extents[index] = extent;
			}
		}

		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		private static unsafe void MinMax([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			// If statement is optimised away, Burst recognises it as compile time constant.
			if(IsAvxSupported)
			{
				// Use optimised AVX code path if available on the CPU.
				MinMax_AVX(array, length, out minimum, out maximum);
			}
			else
			{
				// Use default code path otherwise.
				MinMax_Default(array, length, out minimum, out maximum);
			}
		}

		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		private static unsafe void MinMax_Default([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			minimum = float.MaxValue;
			maximum = float.MinValue;

			for(int i = 0; i < length; i++)
			{
				float value = array[i];
				minimum = min(minimum, value);
				maximum = max(maximum, value);
			}
		}

		/// <summary>
		/// AVX Pathway for the MinMax function.
		/// I found that the burst compiler isn't generating code utilising the YMM registers,
		/// so have developed a pathway specifically for AVX-capable chips that does utilise the 256-bit registers.
		/// </summary>
		/// <remarks>
		/// AVX512 improvements are possible beyond the 512-bit registers:
		/// The reduce instruction would be extremely useful for coalescing the final values into a float.
		/// </remarks>
		/// <param name="array">Pointer to the array to calculate min/max from.</param>
		/// <param name="length">Length of the array to operate on.</param>
		/// <param name="minimum">The minimum value in the array.</param>
		/// <param name="maximum">The maximum value in the array.</param>
		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		private static unsafe void MinMax_AVX([ReadOnly] float* array, int length, out float minimum, out float maximum)
		{
			// Initialise the outputs
			minimum = float.MaxValue;
			maximum = float.MaxValue;

			// Calculate the chunks to operate on, and leftovers
			int remainder = length % 8;
			int lengthFloor = length - remainder;

			// Create registers
			v256 valRegister = new v256();
			v256 minRegister = new v256(float.MaxValue);
			v256 maxRegister = new v256(float.MinValue);

			int chunkSize = sizeof(v256) / sizeof(float);	// Should be constant
			for(int offset = 0; offset < lengthFloor; offset += chunkSize)
			{
				// Store 8 floats from the array
				mm256_store_ps(&array[offset], valRegister);

				// Calculate the min/max for the registers
				minRegister = mm256_min_ps(minRegister, valRegister);
				maxRegister = mm256_max_ps(maxRegister, valRegister);
			}

			// Fill register with last values
			float* vecData = stackalloc float[8];
			for(int i = 0; i < remainder; i++) vecData[i] = array[i];
			mm256_store_ps(vecData, valRegister);

			// Do last min/max
			minRegister = mm256_min_ps(minRegister, valRegister);
			maxRegister = mm256_max_ps(maxRegister, valRegister);

			// Reduce (, reuse, recycle...)
			v128 minLower = mm256_extractf128_ps(minRegister, 0);
			v128 minUpper = mm256_extractf128_ps(minRegister, 1);
			v128 minResult = min_ps(minLower, minUpper);

			v128 maxLower = mm256_extractf128_ps(maxRegister, 0);
			v128 maxUpper = mm256_extractf128_ps(maxRegister, 1);
			v128 maxResult = max_ps(maxLower, maxUpper);

			// Split these to allow for proper auto-vectorisation
			for(int i = 0; i < 4; i++)
			{
				minimum = min(minimum, extract_ps(minResult, 0));
				minResult = srli_epi32(minResult, 32);
			}
			for(int i = 0; i < 4; i++)
			{
				maximum = max(maximum, extract_ps(maxResult, 0));
				maxResult = srli_epi32(maxResult, 32);
			}
		}

		[BurstCompile]
		private struct IndicesJob : IJob
		{
			[WriteOnly] public NativeArray<int> indices;

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
			}
		}

		[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
		private struct VertexJob : IJobParallelFor
		{
			[ReadOnly]  public NativeArray<float> angles;

			public float extentsMin, extentsMax;
			[ReadOnly]  public NativeArray<float> extents;

			[WriteOnly] public NativeSlice<float3> vertices;

			public void Execute(int index)
			{
				float angle = angles[index];
				float2 direction = float2(cos(angle), sin(angle));

				float extent = extents[index];
				extent = unlerp(extentsMin, extentsMax, extent);
				direction *= extent;

				vertices[index] = float3(direction, 0.0f);
			}
		}
	}
}
