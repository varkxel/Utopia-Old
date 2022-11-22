using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

using Unity.Collections;
using Random = Unity.Mathematics.Random;

namespace Utopia.World.Masks
{
	[CustomEditor(typeof(Mask))]
	internal class MaskInspector : Editor
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
		
		private NativeArray<float> result;
		
		public void UpdateTexture()
		{
			Mask mask = target as Mask;
			Debug.Assert(mask != null, nameof(mask) + "!= null");
			
			result = new NativeArray<float>(resolution * resolution, Allocator.Persistent);
			
			mask.Generate(ref random, resolution);
			mask.GetResult(ref result, UpdateTexture_OnMaskGenerated);
		}
		
		private void UpdateTexture_OnMaskGenerated() {
			Color[] image = new Color[resolution * resolution];
			for (int i = 0; i < result.Length; i++) {
				float val = result[i];
				image[i] = new Color(val, val, val, 1.0f);
			}
			result.Dispose();
			
			texture.SetPixels(image);
			texture.Apply();
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