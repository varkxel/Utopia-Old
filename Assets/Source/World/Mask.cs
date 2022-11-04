using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Utopia.Noise;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;

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

			IndicesJob indicesJob = new IndicesJob()
			{
				indices = new NativeArray<int>(((verticesCount - 2) * 3) + 3, Allocator.TempJob)
			};
			JobHandle indicesJobHandle = indicesJob.Schedule();

			anglesHandle.Complete();
			NativeArray<float> angles = anglesJob.angles;
			random = anglesJob.random;
			angles.Sort();

			NativeArray<float3> vertices = new NativeArray<float3>(verticesCount + 1, Allocator.TempJob);
			vertices[0] = float3.zero;
			VertexJob vertexJob = new VertexJob()
			{
				angles = angles,
				vertices = vertices.Slice(1),
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

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
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

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private struct MinMaxJobParallel : IJobParallelFor
		{
			public volatile float minimum;
			public volatile float maximum;

			[ReadOnly] public NativeArray<float> array;

			public void Execute(int index)
			{
				float val = array[index];
				if(val > maximum) maximum = val;
				if(val < minimum) minimum = val;
			}
		}

		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private struct MinMaxJob : IJob
		{
			public float minimum;
			public float maximum;

			[ReadOnly] public NativeArray<float> array;

			public void Execute()
			{
				float length = array.Length;
				for(int i = 0; i < length; i++)
				{
					float val = array[i];
					minimum = min(minimum, val);
					maximum = max(maximum, val);
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
			[ReadOnly]  public NativeArray<float> extents;
			[WriteOnly] public NativeSlice<float3> vertices;

			public void Execute(int index)
			{
				float angle = angles[index];
				float2 direction = float2(cos(angle), sin(angle));

				float extent = extents[index];

				vertices[index] = float3(direction, 0.0f);
			}
		}
	}
}
