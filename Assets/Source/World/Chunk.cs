using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
			Chunk chunk = obj.AddComponent<Chunk>();
			chunk.index = index;
			chunk.size = Generator.instance.chunkSize;

			chunk.Generate();
			return chunk;
		}

		private int MeshSize => size + 1;

		private void Start() { Generate(); }

		public void Generate()
		{
			Generator generator = Generator.instance;
			
			int meshSize = MeshSize;
			int meshSizeSq = meshSize * meshSize;
			NativeArray<double> heightmap = new NativeArray<double>(meshSizeSq, Allocator.TempJob);
			generator.heightmap.CreateJob(index, meshSize, out SimplexFractal2D heightmapGenerator);

			heightmapGenerator.result = heightmap;
			JobHandle heightmapJob = heightmapGenerator.Schedule(heightmap.Length, 4);

			NativeArray<int> indices = new NativeArray<int>(IndicesJob.ResultSize(meshSize), Allocator.TempJob);
			IndicesJob indicesJobData = new IndicesJob()
			{
				size = meshSize,
				results = indices
			};
			JobHandle indicesJob = indicesJobData.Schedule(IndicesJob.ResultSize(meshSize), 6);

			NativeArray<int> biomeMap = new NativeArray<int>(meshSizeSq, Allocator.TempJob);
			generator.biomes.GenerateChunk(index, meshSize, ref biomeMap);
			
			// Has to be single threaded unfortunately,
			// Should change the biome system to use structs / function pointers.
			NativeArray<float> heightmapMultiplier = new NativeArray<float>(biomeMap.Length, Allocator.TempJob);
			heightmapJob.Complete();
			for(int i = 0; i < biomeMap.Length; i++)
			{
				heightmapMultiplier[i] = generator.biomes.biomes[biomeMap[i]].heightMultiplier.Evaluate((float) heightmap[i]);
			}
			biomeMap.Dispose();

			NativeArray<float3> vertices = new NativeArray<float3>(meshSizeSq, Allocator.TempJob);
			VertexJob vertexJobData = new VertexJob()
			{
				size = meshSize,
				heights = heightmap,
				vertices = vertices,
				biomeMultiplier = heightmapMultiplier
			};
			// TODO add heightmap multiplier job as dependency in the future - once it exists.
			JobHandle vertexJob = vertexJobData.Schedule(meshSizeSq, meshSize, heightmapJob);

			indicesJob.Complete();
			vertexJob.Complete();
			Mesh mesh = new Mesh()
			{
				vertices = vertices.Reinterpret<Vector3>().ToArray(),
				triangles = indices.ToArray(),

				indexFormat = IndexFormat.UInt16
			};
			GetComponent<MeshFilter>().mesh = mesh;

			vertices.Dispose();
			indices.Dispose();
			heightmapMultiplier.Dispose();
			heightmap.Dispose();
		}
		
		[BurstCompile]
		private struct IndicesJob : IJobParallelFor
		{
			public static int ResultSize(int size) => (size * size - size) * 3;

			public int size;
			public NativeArray<int> results;

			public void Execute(int i)
			{
				int triIndex = i / 3;
				int triPoint = i % 3;
				
				if(triPoint == 0)
				{
					// First point of triangle is direct
					results[i] = triIndex;
				}
				else
				{
					int square = triIndex / 2;
					square *= 2;

					// Move to next Y
					int index = square + size;
					// Go backwards - clockwise
					index -= triPoint - 1;
					// Compensate
					index += 1;

					results[i] = index;
				}
			}
		}

		[BurstCompile]
		private struct VertexJob : IJobParallelFor
		{
			public int size;

			[ReadOnly]  public NativeArray<double> heights;
			[ReadOnly]  public NativeArray<float> biomeMultiplier;

			[WriteOnly] public NativeArray<float3> vertices;

			public void Execute(int index)
			{
				int2 index2D = int2(index % size, index / size);

				float val = (float) heights[index];
				val *= biomeMultiplier[index];
				vertices[index] = float3(index2D.x, val, index2D.y);
			}
		}
	}
}