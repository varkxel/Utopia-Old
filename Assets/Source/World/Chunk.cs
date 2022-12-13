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

		internal static Chunk Create(GameObject obj, int2 index)
		{
			obj.AddComponent<MeshFilter>();
			MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
			renderer.material = Generator.instance.chunkMaterial;
			
			Chunk chunk = obj.AddComponent<Chunk>();
			chunk.index = index;
			chunk.size = Generator.instance.chunkSize;

			chunk.Generate();
			return chunk;
		}

		public void Generate()
		{
			JobHandle heightmapJob = GenerateHeightmap();
			JobHandle indicesJob = GenerateIndices();
			JobHandle biomeJob = GenerateBiomes(out UnityAction biomeJobCallback);
			

			NativeArray<float3> vertices = new NativeArray<float3>(size * size, Allocator.TempJob);
			VertexJob vertexJobData = new VertexJob()
			{
				size = size,
				heights = heightmap,
				vertices = vertices
			};
			// TODO add heightmap multiplier job as dependency in the future - once it exists.
			JobHandle vertexJob = vertexJobData.Schedule(size * size, size, heightmapJob);

			indicesJob.Complete();
			vertexJob.Complete();
			Mesh mesh = new Mesh()
			{
				vertices = vertices.Reinterpret<Vector3>().ToArray(),
				triangles = indices.ToArray(),

				indexFormat = IndexFormat.UInt16
			};
			mesh.RecalculateNormals();
			GetComponent<MeshFilter>().mesh = mesh;

			vertices.Dispose();
			indices.Dispose();
			heightmap.Dispose();
		}

		private NativeArray<double> heightmap;

		private JobHandle GenerateHeightmap()
		{
			heightmap = new NativeArray<double>(size * size, Allocator.TempJob);
			
			Generator.instance.heightmap.CreateJob(index, size, out SimplexFractal2D heightmapGenerator);
			heightmapGenerator.result = heightmap;
			
			return heightmapGenerator.Schedule(heightmap.Length, 4);
		}

		private NativeList<int> indices;

		private JobHandle GenerateIndices()
		{
			indices = new NativeList<int>(Allocator.TempJob);
			IndicesJob indicesJobData = new IndicesJob()
			{
				size = size,
				results = indices
			};
			return indicesJobData.Schedule();
		}

		private NativeArray<float4> biomeMap;

		private JobHandle GenerateBiomes(out UnityAction completionCallback)
			=> Generator.instance.biomes.GenerateChunk
		(
			index, size,
			out biomeMap,
			out completionCallback,
			persistent: false
		);
		
		[BurstCompile]
		private struct IndicesJob : IJob
		{
			public int size;
			public NativeList<int> results;

			public void Execute()
			{
				for(int y = 0; y < size - 1; y++)
				{
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