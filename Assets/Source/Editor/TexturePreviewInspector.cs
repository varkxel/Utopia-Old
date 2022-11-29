using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor;

namespace Utopia
{
	internal abstract class TexturePreviewInspector : Editor
	{
		// Texture field
		protected Texture2D texture;
		protected const int resolution = 512;
		
		/// <summary>
		/// Uploads the given colour array to the texture object.
		/// </summary>
		/// <param name="image">Image colour array to upload.</param>
		protected void UploadTexture(in Color[] image)
		{
			texture.SetPixels(image);
			texture.Apply();
		}
		
		protected virtual void Awake()
		{
			texture = new Texture2D(resolution, resolution, DefaultFormat.HDR, TextureCreationFlags.DontUploadUponCreate);
			UpdateTexture();
		}
		
		/// <summary>
		/// Called when the texture should be updated.
		/// </summary>
		public abstract void UpdateTexture();
		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			// Draw title
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
			
			// Draw texture
			EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), texture);
			
			// Update button
			if(GUILayout.Button("Update Preview")) UpdateTexture();
		}
	}
}