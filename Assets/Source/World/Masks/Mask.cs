using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;

using MathsUtils;

namespace Utopia.World.Masks
{
	[CreateAssetMenu(menuName = Generator.AssetPath + "Mask", fileName = "Mask"), BurstCompile]
	public class Mask : ScriptableObject
	{
		public const int batchSize = MathsUtil.MinMax_MaxBatch;
		
		[Header("Mesh Settings")]
		// ReSharper disable once PossibleLossOfFraction
		[Range(batchSize, ushort.MaxValue / 2)]
		public ushort complexity = 256;
		
		[Header("Noise Generation")]
		public float scale = 2.0f;
		public uint octaves = 4;
		public float gain = 0.5f;
		public float lacunarity = 2.0f;
		
		[Header("Levels")]
		public float seaLevel = 0.2f;
		public float mainlandLevel = 0.5f;
		
		public RenderTexture gpuResult { get; private set; }
		public bool generated { get; private set; } = false;
		
		#region Shader Variables/Parameters
		
		// Transform matrices
		private static readonly float4x4 matrixTransform = float4x4.TRS(float3(0), quaternion.identity, float3(1));
		private static readonly float4x4 matrixLook = float4x4.TRS(float3(0, 0, -1), quaternion.identity, float3(1));
		private static readonly float4x4 matrixOrtho = float4x4.Ortho(2, 2, 0.01f, 2);

		private const string shader = "Hidden/Utopia/World/MaskGenerator";
		private static Material material;
		
		private static readonly int mainlandProperty = Shader.PropertyToID("_Mainland");
		private static readonly int oceanProperty = Shader.PropertyToID("_Ocean");
		
		#endregion
		
		public void Generate(ref Random random, int size)
		{
			CommandBuffer commandBuffer = new CommandBuffer()
			{
				name = "Generate Mask"
			};
			
			int verticesCount = complexity;
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
				scale = this.scale,
				octaves = this.octaves,
				lacunarity = this.lacunarity,
				gain = this.gain
			};
			JobHandle extentsHandle = extentsJob.Schedule(verticesCount, batchSize, anglesHandle);
			
			// Find maximum and minimum extent for normalisation
			NativeArray<float> extentsMinMax = new NativeArray<float>(2, Allocator.TempJob);
			MathsUtil.MinMaxJob minMaxJob = new MathsUtil.MinMaxJob()
			{
				array = extents,
				minMax = extentsMinMax
			};
			JobHandle minMaxHandle = minMaxJob.Schedule(extentsHandle);

			// Calculate the vertices / normalise
			NativeArray<float3> vertices = new NativeArray<float3>(verticesCount + 1, Allocator.TempJob);
			vertices[0] = float3.zero;

			int smoothingAmount = verticesCount / 8;
			
			// Setup GPU
			gpuResult = new RenderTexture(size, size, 0, RenderTextureFormat.RFloat)
			{
				useMipMap = false,
				antiAliasing = 4
			};
			
			// Setup material
			if(material == null)
			{
				material = new Material(Shader.Find(shader));
			}
			material.SetFloat(oceanProperty, seaLevel);
			material.SetFloat(mainlandProperty, mainlandLevel);
			
			extentsHandle.Complete();

			int extentsFinalIndex = extents.Length - 1;
			SmoothJob smoothingJob = new SmoothJob()
			{
				array = extents.Slice(extentsFinalIndex - smoothingAmount, smoothingAmount),
				smoothSamples = smoothingAmount,
				startSample = extents[^(smoothingAmount + 1)],
				endSample = extents[0]
			};
			int smoothingSliceSize = smoothingJob.array.Length;
			JobHandle smoothingJobHandle = smoothingJob.Schedule(smoothingSliceSize, min(smoothingSliceSize, 8), minMaxHandle);
			
			VertexJob vertexJob = new VertexJob()
			{
				angles = angles,
				vertices = vertices.Slice(1),
				extents = extents,
				extentsMinMax = extentsMinMax
			};
			// Schedule parallel across all cores - completely parallel job, though nothing can really run while it's running.
			JobHandle vertexJobHandle = vertexJob.Schedule(verticesCount, batchSize, JobHandle.CombineDependencies(anglesHandle, smoothingJobHandle));

			commandBuffer.SetRenderTarget(gpuResult);
			commandBuffer.ClearRenderTarget(false, true, Color.black);
			commandBuffer.SetViewProjectionMatrices(matrixLook, matrixOrtho);

			vertexJobHandle.Complete();
			extentsMinMax.Dispose();

			extents.Dispose();
			angles.Dispose();

			indicesHandle.Complete();

			Mesh mesh = new Mesh()
			{
				vertices = vertices.Reinterpret<Vector3>().ToArray(),
				triangles = indices.ToArray(),

				indexFormat = IndexFormat.UInt16
			};
			commandBuffer.DrawMesh(mesh, matrixTransform, material);
			Graphics.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Clear();

			vertices.Dispose();
			indices.Dispose();
			
			commandBuffer.Dispose();
		}
		
		public void GetResult(ref NativeArray<float> result, UnityAction onCompleted = null)
		{
			AsyncGPUReadback.RequestIntoNativeArray(ref result, gpuResult, 0, request =>
			{
				if(request.hasError)
				{
					Debug.LogError("Error requesting island mask data from GPU.");
					return;
				}
				generated = true;
				gpuResult.DiscardContents();
				
				onCompleted?.Invoke();
			});
		}
	}
}
