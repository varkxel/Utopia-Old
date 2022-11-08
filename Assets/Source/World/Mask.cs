using UnityEngine;
using UnityEngine.Rendering;

using Unity.Burst;
using Unity.Burst.Intrinsics;
using static Unity.Burst.Intrinsics.X86.Avx;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

using Unity.Jobs;

using Utopia.Noise;

namespace Utopia.World
{
	[BurstCompile]
	public class Mask
	{
		private const int batchSize = AVXUtils.MinMax_batchSize;

		[System.Serializable]
		public struct GenerationSettings
		{
			[Header("Mesh Settings")]
			[Range(batchSize, 65535)] public int complexity;

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

		private static readonly float4x4 matrixTransform = float4x4.TRS(float3(0), quaternion.identity, float3(1.0f));
		private static readonly float4x4 matrixLook = float4x4.TRS(float3(0, 0, -1), quaternion.identity, float3(1));
		private static readonly float4x4 matrixOrtho = float4x4.Ortho(2, 2, 0.01f, 2);

		private const string shader = "Hidden/Utopia/World/MaskGenerator";
		private Material material;

		public Mask(int size, GenerationSettings settings)
		{
			this.settings = settings;

			material = new Material(Shader.Find(shader));
			result = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat)
			{
				filterMode = FilterMode.Trilinear
			};
		}

		public void Generate(ref Random random)
		{
			int verticesCount = settings.complexity;
			verticesCount -= verticesCount % batchSize;

			NativeArray<float> angles = new NativeArray<float>(verticesCount, Allocator.TempJob);
			AnglesJob anglesJob = new AnglesJob()
			{
				random = random,
				angles = angles,
				angleCount = verticesCount
			};
			JobHandle anglesHandle = anglesJob.Schedule();

			// Schedule indices job whilst angles job runs
			NativeArray<int> indices = new NativeArray<int>(((verticesCount - 2) * 3) + 3, Allocator.TempJob);
			IndicesJob indicesJob = new IndicesJob()
			{
				indices = indices
			};
			JobHandle indicesHandle = indicesJob.Schedule();

			NativeArray<float> extents = new NativeArray<float>(verticesCount, Allocator.TempJob);
			ExtentsJob extentsJob = new ExtentsJob()
			{
				extents = extents,
				angles = angles,

				seed = random.NextFloat(),
				settings = settings
			};
			JobHandle extentsHandle = extentsJob.Schedule(verticesCount, batchSize, anglesHandle);

			NativeArray<float3> vertices = new NativeArray<float3>(verticesCount + 1, Allocator.TempJob);
			vertices[0] = float3.zero;

			// Calculate the min/max whilst the extents are sorted, using a duplicated array.
			float extentsMin, extentsMax;
			unsafe
			{
				MinMax((float*) extents.GetUnsafePtr(), extents.Length, out extentsMin, out extentsMax);
			}

			// Normalisation done in vertex job
			VertexJob vertexJob = new VertexJob()
			{
				angles = angles,
				vertices = vertices.Slice(1),
				extents = extents,
				
				extentsMin = extentsMin,
				extentsMax = extentsMax
			};
			JobHandle vertexJobHandle = vertexJob.Schedule(verticesCount, batchSize);

			commandBuffer.SetRenderTarget(result);
			commandBuffer.ClearRenderTarget(false, true, Color.black);
			commandBuffer.SetViewProjectionMatrices(matrixLook, matrixOrtho);

			vertexJobHandle.Complete();

			extents.Dispose();
			angles.Dispose();

			indicesHandle.Complete();

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

		[BurstCompile(FloatPrecision.Medium, FloatMode.Default)]
		private struct AnglesJob : IJob
		{
			public Random random;

			public int angleCount;
			[WriteOnly] public NativeArray<float> angles;

			public void Execute()
			{
				const float start = -PI + EPSILON;
				float baseOffset = (2.0f * PI) / (float) angleCount;

				float currentAngle = start;
				float lastOffset = 0.0f;

				for(int i = 0; i < angleCount; i++)
				{
					angles[i] = currentAngle;

					float offset = random.NextFloat(0.0f, baseOffset);
					currentAngle += (baseOffset - lastOffset) + offset;

					lastOffset = offset;
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
		private static unsafe void MinMax(float* array, int length, out float minimum, out float maximum)
		{
			// If statement is optimised away, Burst recognises it as compile time constant.
			if(IsAvxSupported)
			{
				// Use optimised AVX code path if available on the CPU.
				AVXUtils.MinMax(array, length, out minimum, out maximum);
			}
			else
			{
				// Use default code path otherwise.
				minimum = float.MaxValue;
				maximum = float.MinValue;

				for(int i = 0; i < length; i++)
				{
					float value = array[i];
					minimum = min(minimum, value);
					maximum = max(maximum, value);
				}
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
