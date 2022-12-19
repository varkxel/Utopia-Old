using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Events;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;
using Utopia.Noise;

namespace Utopia.World
{
	[RequireComponent(typeof(MeshFilter))]
	public class Chunk : MonoBehaviour
	{
		public int2 index;
		public int size;
		private int meshSize;

		internal static Chunk Create(GameObject obj, int2 index)
		{
			obj.AddComponent<MeshFilter>();
			MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
			renderer.material = Generator.instance.chunkMaterial;
			
			Chunk chunk = obj.AddComponent<Chunk>();
			chunk.index = index;
			chunk.size = Generator.instance.chunkSize;
			chunk.meshSize = chunk.size + 1;

			chunk.Generate();
			return chunk;
		}

		public void Generate()
		{
			JobHandle indicesJob = GenerateIndices();
			JobHandle biomeJob = GenerateBiomes();

			JobHandle heightmapJob = GenerateHeightmap();
			JobHandle heightmapMultiplierJob = ApplyMultiplier(heightmapJob, biomeJob);

			JobHandle vertexJob = GenerateVertices(heightmapMultiplierJob);
			JobHandle meshDependency = JobHandle.CombineDependencies(indicesJob, vertexJob);
			
			meshDependency.Complete();

			Generator.instance.biomes.GenerateChunk_OnComplete();
			
			Mesh mesh = new Mesh()
			{
				vertices = vertices.Reinterpret<Vector3>().ToArray(),
				triangles = indices.ToArray(),

				indexFormat = IndexFormat.UInt16
			};
			mesh.SetUVs(0, biomeMap);
			mesh.RecalculateNormals();
			GetComponent<MeshFilter>().mesh = mesh;
			
			heightmap.Dispose();
			biomeMap.Dispose();
			vertices.Dispose();
			indices.Dispose();
		}

		private NativeArray<double> heightmap;
		private NativeArray<float4> biomeMap;
		private NativeArray<float3> vertices;
		private NativeList<int> indices;

		private JobHandle GenerateHeightmap()
		{
			heightmap = new NativeArray<double>(meshSize * meshSize, Allocator.TempJob);
			
			Generator.instance.heightmap.CreateJob(index, meshSize, out SimplexFractal2D heightmapGenerator);
			heightmapGenerator.result = heightmap;
			
			return heightmapGenerator.Schedule(heightmap.Length, 4);
		}

		private JobHandle GenerateIndices()
		{
			indices = new NativeList<int>(Allocator.TempJob);
			IndicesJob indicesJobData = new IndicesJob()
			{
				size = meshSize,
				results = indices
			};
			return indicesJobData.Schedule();
		}

		private JobHandle GenerateBiomes()
			=> Generator.instance.biomes.GenerateChunk
		(
			index, meshSize,
			out biomeMap,
			persistent: false
		);

		private JobHandle ApplyMultiplier(JobHandle heightmapJob, JobHandle biomeJob)
		{
			Generator generator = Generator.instance;

			BiomeMap.ModifierJob modifierJob = new BiomeMap.ModifierJob()
			{
				heightmap = heightmap,
				biomes = biomeMap,
				blend = generator.biomes.blendThreshold,
				curves = generator.biomes.curves
			};
			return modifierJob.Schedule(heightmap.Length, 4, JobHandle.CombineDependencies(heightmapJob, biomeJob));
		}

		private JobHandle GenerateVertices(JobHandle heightmapJob)
		{
			int vertexArraySize = meshSize * meshSize;
			vertices = new NativeArray<float3>(vertexArraySize, Allocator.TempJob);
			VertexJob vertexJobData = new VertexJob()
			{
				size = meshSize,
				heights = heightmap,
				vertices = vertices
			};
			return vertexJobData.Schedule(vertexArraySize, 32, heightmapJob);
		}

		[BurstCompile]
		private struct IndicesJob : IJob
		{
			/*
				This could be quicker.
				TODO Look into making this parallel.
			*/

			public int size;
			public NativeList<int> results;

			public void Execute()
			{
				for(int y = 0; y < size - 1; y++)
				for(int x = 0; x < size - 1; x++)
				{
					results.Add(Pos(x, y, size));
					results.Add(Pos(x + 1, y + 1, size));
					results.Add(Pos(x + 1, y, size));

					results.Add(Pos(x + 1, y + 1, size));
					results.Add(Pos(x, y, size));
					results.Add(Pos(x, y + 1, size));
				}
			}

			[BurstCompile]
			private static int Pos(int x, int y, int size) => x + (y * size);
		}

		[BurstCompile]
		private struct VertexJob : IJobParallelFor
		{
			public int size;

			[ReadOnly]  public NativeArray<double> heights;
			[WriteOnly] public NativeArray<float3> vertices;

			public void Execute(int index)
			{
				int2 index2D = int2(index % size, index / size);

				float val = (float) heights[index];
				vertices[index] = float3(index2D.x, val, index2D.y);
			}
		}
	}
}