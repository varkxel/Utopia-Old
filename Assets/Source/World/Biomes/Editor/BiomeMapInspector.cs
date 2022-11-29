using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Utopia.World.Biomes
{
	[CustomEditor(typeof(BiomeMap))]
	internal sealed class BiomeMapInspector : TexturePreviewInspector
	{
		private Random random;
		
		private const float colourOffset = 0.2f;
		private const int colourSteps = (int) (1.0f / colourOffset);
		private static readonly Color[] colourList = new Color[]
		{
			new Color(0, 0, 0, 1),
			new Color(1, 0, 0, 1), new Color(0, 1, 0, 1), new Color(0, 0, 1, 1),
			new Color(1, 1, 0, 1), new Color(1, 0, 1, 1), new Color(0, 1, 1, 1),
			new Color(1, 1, 1, 1)
		};
		
		protected override void Awake()
		{
			random.InitState();
			
			base.Awake();
		}
		
		public override void UpdateTexture()
		{
			BiomeMap map = target as BiomeMap;
			Debug.Assert(map != null, nameof(map) + " != null");
			
			NativeArray<int> result = new NativeArray<int>(resolution * resolution, Allocator.TempJob);
			map.GenerateChunk(int2.zero, resolution, ref result);
			
			Color[] colours = new Color[result.Length];
			for(int i = 0; i < result.Length; i++)
			{
				int colourIndex = result[i] % colourList.Length;
				int steps = (result[i] / colourList.Length) % colourSteps;
				
				float4 colour = (Vector4) colourList[colourIndex];
				colour += colourOffset * (float) steps;
				colour = math.clamp(colour, 0.0f, 1.0f);
				colours[i] = (Vector4) colour;
			}
			result.Dispose();
			
			UploadTexture(colours);
		}
	}
}