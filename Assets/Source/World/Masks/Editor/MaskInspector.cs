using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Utopia.World.Masks {
	/// <summary>
	///     An inspector to visualise Island Masks.
	/// </summary>
	[CustomEditor(typeof(Mask))]
	internal sealed class MaskInspector : TexturePreviewInspector {
		/// <summary>
		///     Random instance to be used for the preview generation.
		/// </summary>
		private Random random;

		/// <summary>
		///     Internal result array for the GPU data to get written to.
		/// </summary>
		private NativeArray<float> result;

		protected override void Awake() {
			random.InitState();

			base.Awake();
		}

		public override void UpdateTexture() {
			Mask mask = target as Mask;
			Debug.Assert(mask != null, nameof(mask) + " != null");

			result = new NativeArray<float>(resolution * resolution, Allocator.Persistent);

			mask.Generate(ref random, resolution);
			mask.GetResult(ref result, UpdateTexture_OnMaskGenerated);
		}

		/// <summary>
		///     GPU callback for once the preview texture has been read back from the GPU.
		///     Finishes off the texture update.
		/// </summary>
		private void UpdateTexture_OnMaskGenerated() {
			Color[] image = new Color[resolution * resolution];
			for (int i = 0; i < result.Length; i++) {
				float val = result[i];
				image[i] = new Color(val, val, val, 1.0f);
			}

			result.Dispose();

			UploadTexture(image);
		}
	}
}