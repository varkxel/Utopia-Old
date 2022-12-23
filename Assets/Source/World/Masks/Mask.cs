using MathsUtils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;

namespace Utopia.World.Masks {
	/// <summary>
	///     A mask to be applied to any generated heightmap to transform it into an island shape.
	/// </summary>
	[CreateAssetMenu(menuName = Generator.AssetPath + "Mask", fileName = "Mask")]
	[BurstCompile]
	public class Mask : ScriptableObject {
		/// <summary>
		///     Maximum batch size.
		///     The <see cref="complexity" /> value needs to be a multiple of this.
		/// </summary>
		public const int batchSize = MathsUtil.MinMax_Batch;

		/// <summary>
		///     How many points to generate on the island mask mesh.
		/// </summary>
		[Header("Mesh Settings")] public int complexity = 256;

		// Noise generation settings
		[Header("Noise Generation")] public float scale = 2.0f;

		public uint octaves = 4;
		public float gain = 0.5f;
		public float lacunarity = 2.0f;

		// Mask shader parameters
		[Header("Levels")] public float seaLevel = 0.2f;

		public float mainlandLevel = 0.5f;

		/// <summary>
		///     Raw GPU RenderTexture for the result to be drawn to.
		/// </summary>
		public RenderTexture gpuResult { get; private set; }

		/// <summary>
		///     Whether the mask has been generated or not.
		/// </summary>
		public bool generated { get; private set; }

		/// <summary>
		///     Generates the mask for the world.
		/// </summary>
		/// <param name="random">The random instance to utilise.</param>
		/// <param name="size">The size of the mask to generate.</param>
		public void Generate(ref Random random, int size) {
			// Create command buffer
			CommandBuffer commandBuffer = new CommandBuffer {
				name = "Generate Mask"
			};

			// Calculate vertex count as a multiple of the batch size
			int verticesCount = complexity;
			verticesCount -= verticesCount % batchSize;

			// Create the angles for the mesh
			NativeArray<float> angles = new NativeArray<float>(verticesCount, Allocator.TempJob);
			AnglesJob anglesJob = new AnglesJob {
				random = random,
				angles = angles,
				angleCount = verticesCount
			};
			JobHandle anglesHandle = anglesJob.Schedule();

			// Schedule the mesh indices creation job whilst angles job runs
			NativeArray<int> indices = new NativeArray<int>((verticesCount - 2) * 3 + 3, Allocator.TempJob);
			IndicesJob indicesJob = new IndicesJob {
				indices = indices
			};
			JobHandle indicesHandle = indicesJob.Schedule();

			// Generate the extents with 1D noise
			NativeArray<float> extents = new NativeArray<float>(verticesCount, Allocator.TempJob);
			ExtentsJob extentsJob = new ExtentsJob {
				extents = extents,
				angles = angles,

				seed = random.NextFloat(),
				scale = scale,
				octaves = octaves,
				lacunarity = lacunarity,
				gain = gain
			};
			JobHandle extentsHandle = extentsJob.Schedule(verticesCount, batchSize, anglesHandle);

			// Find maximum and minimum extent for normalisation
			NativeArray<float> extentsMinMax = new NativeArray<float>(2, Allocator.TempJob);
			MathsUtil.MinMaxJob minMaxJob = new MathsUtil.MinMaxJob {
				array = extents,
				minMax = extentsMinMax
			};
			JobHandle minMaxHandle = minMaxJob.Schedule(extentsHandle);

			// Calculate the vertices / normalise from the given values
			NativeArray<float3> vertices = new NativeArray<float3>(verticesCount + 1, Allocator.TempJob);
			vertices[0] = float3.zero;

			// Setup GPU
			gpuResult = new RenderTexture(size, size, 0, RenderTextureFormat.RFloat) {
				useMipMap = false,
				antiAliasing = 4
			};

			// Setup material
			if (material == null) material = new Material(Shader.Find(shader));
			material.SetFloat(oceanProperty, seaLevel);
			material.SetFloat(mainlandProperty, mainlandLevel);

			extentsHandle.Complete();

			// Smoothing
			int smoothingAmount = verticesCount / 8;
			SmoothJob smoothingJob = new SmoothJob {
				array = extents.Slice(extents.Length - 1 - smoothingAmount, smoothingAmount),
				smoothSamples = smoothingAmount,
				startSample = extents[^(smoothingAmount + 1)],
				endSample = extents[0]
			};
			int smoothingSliceSize = smoothingJob.array.Length;
			JobHandle smoothingJobHandle =
				smoothingJob.Schedule(smoothingSliceSize, min(smoothingSliceSize, 8), minMaxHandle);

			// Generate the vertices for the mesh
			VertexJob vertexJob = new VertexJob {
				angles = angles,
				vertices = vertices.Slice(1),
				extents = extents,
				extentsMinMax = extentsMinMax
			};
			// Schedule parallel across all cores - completely parallel job, though nothing can really run while it's running.
			JobHandle vertexJobHandle = vertexJob.Schedule(verticesCount, batchSize,
				JobHandle.CombineDependencies(anglesHandle, smoothingJobHandle));

			// Setup GPU render pipeline
			commandBuffer.SetRenderTarget(gpuResult);
			commandBuffer.ClearRenderTarget(false, true, Color.black);
			commandBuffer.SetViewProjectionMatrices(matrixLook, matrixOrtho);

			// Await mesh creation prerequisites / Cleanup
			vertexJobHandle.Complete();

			extentsMinMax.Dispose();
			extents.Dispose();
			angles.Dispose();

			indicesHandle.Complete();

			// Create the mesh
			Mesh mesh = new Mesh {
				vertices = vertices.Reinterpret<Vector3>().ToArray(),
				triangles = indices.ToArray(),

				indexFormat = IndexFormat.UInt16
			};

			// Render the mesh
			commandBuffer.DrawMesh(mesh, matrixTransform, material);
			Graphics.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Clear();

			// Cleanup
			vertices.Dispose();
			indices.Dispose();

			commandBuffer.Dispose();
		}

		/// <summary>
		///     Gets the resulting texture asynchronously from the GPU.
		/// </summary>
		/// <param name="result">The array to write the results into.</param>
		/// <param name="onCompleted">
		///     The callback for when the results have been read back from the GPU.
		///     (Optional)
		/// </param>
		public void GetResult(ref NativeArray<float> result, UnityAction onCompleted = null) {
			AsyncGPUReadback.RequestIntoNativeArray(ref result, gpuResult, 0, request => {
				// Error check
				if (request.hasError) {
					Debug.LogError("Error requesting island mask data from GPU.");
					return;
				}

				// Cleanup
				gpuResult.DiscardContents();

				// Set generated state
				generated = true;

				// Invoke the callback
				onCompleted?.Invoke();
			});
		}

		#region Shader Variables/Parameters

		// Transform matrices
		private static readonly float4x4 matrixTransform = float4x4.TRS(float3(0), quaternion.identity, float3(1));
		private static readonly float4x4 matrixLook = float4x4.TRS(float3(0, 0, -1), quaternion.identity, float3(1));
		private static readonly float4x4 matrixOrtho = float4x4.Ortho(2, 2, 0.01f, 2);

		// Shader
		private const string shader = "Hidden/Utopia/World/MaskGenerator";
		private static Material material;

		// Shader properties
		private static readonly int mainlandProperty = Shader.PropertyToID("_Mainland");
		private static readonly int oceanProperty = Shader.PropertyToID("_Ocean");

		#endregion
	}
}