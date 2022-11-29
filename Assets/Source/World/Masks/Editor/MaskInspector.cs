using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Random = Unity.Mathematics.Random;

namespace Utopia.World.Masks
{
	[CustomEditor(typeof(Mask))]
	internal sealed class MaskInspector : TexturePreviewInspector
	{
		private Random random;
		
		protected override void Awake()
		{
			random.InitState();
			
			base.Awake();
		}
		
		private NativeArray<float> result;
		
		public override void UpdateTexture()
		{
			Mask mask = target as Mask;
			Debug.Assert(mask != null, nameof(mask) + " != null");
			
			result = new NativeArray<float>(resolution * resolution, Allocator.Persistent);
			
			mask.Generate(ref random, resolution);
			mask.GetResult(ref result, UpdateTexture_OnMaskGenerated);
		}
		
		private void UpdateTexture_OnMaskGenerated()
		{
			Color[] image = new Color[resolution * resolution];
			for(int i = 0; i < result.Length; i++)
			{
				float val = result[i];
				image[i] = new Color(val, val, val, 1.0f);
			}
			result.Dispose();
			
			UploadTexture(image);
		}
	}
}