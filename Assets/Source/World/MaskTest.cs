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
			Mask mask = new Mask(1024, new Mask.Settings()
			{
				complexity = 16384,
				gain = 0.5f,
				lacunarity = 2.0f,
				octaves = 4,
				scale = 1.0f,
				
				seaLevel = 0.4f,
				mainlandLevel = 0.5f
			});
			Random random = new Random(34724);
			mask.Generate(ref random);

			tex = new Texture2D(1024, 1024, TextureFormat.RGBAFloat, false);

			Graphics.CopyTexture(mask.result, tex);
			mask.result.DiscardContents();

			GetComponent<SpriteRenderer>().sprite = Sprite.Create(tex, Rect.MinMaxRect(0, 0, 1024, 1024), Vector2.zero, 128);
		}
	}
}