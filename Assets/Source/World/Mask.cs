using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;

namespace Utopia.World
{
	public class Mask
	{
		public int size;
		public int complexity;

		public RenderTexture result { get; private set; }

		private CommandBuffer commandBuffer = new CommandBuffer()
		{
			name = "MaskGenerator"
		};

		private static readonly float4x4 matrixTransform = float4x4.TRS(float3(0), quaternion.identity, float3(1));
		private static readonly float4x4 matrixLook = float4x4.TRS(float3(0, 0, -1), quaternion.identity, float3(1));
		private static readonly float4x4 matrixOrtho = float4x4.Ortho(2, 2, 0.01f, 2);

		private const string shader = "Hidden/Utopia/World/MaskGenerator";
		private Material material;

		public Mask(int size, int complexity = 512)
		{
			this.size = size;
			this.complexity = complexity;

			material = new Material(Shader.Find(shader));
			result = new RenderTexture(size, size, 0, RenderTextureFormat.RFloat);
		}

		public void Generate(ref Random random)
		{
			int vertices = random.NextInt(4, complexity);
			vertices += vertices % 4;

			AnglesJob anglesJob = new AnglesJob()
			{
				random = random,
				angles = new NativeArray<float>(vertices, Allocator.TempJob)
			};
			JobHandle anglesHandle = anglesJob.Schedule(vertices, 4);
			anglesHandle.Complete();
			random = anglesJob.random;

			NativeArray<float> angles = anglesJob.angles;
			JobHandle sortJob = angles.SortJob().Schedule();

			commandBuffer.SetRenderTarget(result);
			commandBuffer.ClearRenderTarget(false, true, Color.clear);
			Graphics.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Clear();

			GraphicsBuffer vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, vertices, sizeof(float));
			commandBuffer.SetViewProjectionMatrices(matrixLook, matrixOrtho);

			sortJob.Complete();
			vertexBuffer.SetData(angles);
			commandBuffer.DrawProcedural
			(
				vertexBuffer,
				matrixTransform,
				material, 0,
				MeshTopology.Triangles, vertices
			);
		}

		[BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
		private struct AnglesJob : IJobParallelFor
		{
			public Random random;

			[WriteOnly] public NativeArray<float> angles;

			public void Execute(int index)
			{
				angles[index] = random.NextFloat(-PI + EPSILON, PI);
			}
		}
	}
}
