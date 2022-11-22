using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

using Utopia.Noise;

namespace Utopia.World
{
	[CustomEditor(typeof(NoiseMap))]
	internal class NoiseMapInspector : Editor
	{
		private Random random;
		
		private Texture2D texture;
		private const int resolution = 512;
		
		private void Awake()
		{
			random.InitState();
			
			texture = new Texture2D(resolution, resolution, DefaultFormat.HDR, TextureCreationFlags.DontUploadUponCreate);
			UpdateTexture();
		}
		
		public void UpdateTexture()
		{
			NoiseMap noiseMap = target as NoiseMap;
			Debug.Assert(noiseMap != null, nameof(noiseMap) + " != null");
			
			// Generate the noise map to display
			NativeArray<double> result = new NativeArray<double>(resolution * resolution, Allocator.TempJob);
			SimplexFractal2D generator = new SimplexFractal2D()
			{
				settings = noiseMap.settings,
				index = new int2(0, 0),
				size = resolution,
				origin = random.NextDouble2(-32768, 32768),
				result = result
			};
			generator.Initialise(ref random);
			JobHandle generatorHandle = generator.Schedule(resolution * resolution, 4);
			
			// Allocate managed array while job is running
			Color[] image = new Color[resolution * resolution];
			
			// Await job to finish
			generatorHandle.Complete();
			generator.octaveOffsets.Dispose();
			
			// Convert doubles to float for texture
			for(int i = 0; i < result.Length; i++)
			{
				float value = (float) result[i];
				image[i] = new Color(value, value, value, 1.0f);
			}
			
			// Apply result
			texture.SetPixels(image);
			texture.Apply();
			
			// Free noisemap
			result.Dispose();
		}
		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
			
			if(GUILayout.Button("Update Preview"))
			{
				UpdateTexture();
			}
			
			EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), texture);
		}
	}
}