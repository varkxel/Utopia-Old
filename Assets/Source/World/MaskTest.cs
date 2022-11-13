using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = Unity.Mathematics.Random;

namespace Utopia.World
{
	[RequireComponent(typeof(SpriteRenderer))]
	public class MaskTest : MonoBehaviour
	{
		public Texture2D tex;

		public int complexity = 4096;
		
		void Start()
		{
			Mask mask = new Mask();
			Random random = new Random(34724);
			mask.Generate(ref random);

			tex = new Texture2D(1024, 1024, TextureFormat.RGBAFloat, false);

			Graphics.CopyTexture(mask.gpuResult, tex);
			mask.gpuResult.DiscardContents();

			GetComponent<SpriteRenderer>().sprite = Sprite.Create(tex, Rect.MinMaxRect(0, 0, 1024, 1024), Vector2.zero, 128);
		}
	}
}