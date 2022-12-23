using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Utopia {
	/// <summary>
	///     A base inspector for preview inspectors to extend from.
	/// </summary>
	internal abstract class TexturePreviewInspector : Editor {
		protected const int resolution = 512;

		/// <summary>
		///     Whether the texture should be updated on each UI update or not.
		/// </summary>
		private bool alwaysUpdate;

		// Texture field
		protected Texture2D texture;

		protected virtual void Awake() {
			texture = new Texture2D(resolution, resolution, DefaultFormat.HDR, TextureCreationFlags.None);
			if (Application.isPlaying) UpdateTexture();
		}

		/// <summary>
		///     Uploads the given colour array to the texture object.
		/// </summary>
		/// <param name="image">Image colour array to upload.</param>
		protected void UploadTexture(in Color[] image) {
			texture.SetPixels(image);
			texture.Apply();
		}

		/// <summary>
		///     Called when the texture should be updated.
		/// </summary>
		public abstract void UpdateTexture();

		public override void OnInspectorGUI() {
			// Draw base inspector
			base.OnInspectorGUI();

			// Draw title
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

			// Toggle whether the preview should always update
			alwaysUpdate = EditorGUILayout.Toggle("Always Update", alwaysUpdate);

			// Return if not playing as cannot generate
			if (!Application.isPlaying) {
				EditorGUILayout.HelpBox("Play the game to view the preview.", MessageType.Info);
				return;
			}

			// Manual / Auto update
			if (alwaysUpdate || GUILayout.Button("Update Preview")) UpdateTexture();

			// Draw texture
			EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), texture);
		}
	}
}