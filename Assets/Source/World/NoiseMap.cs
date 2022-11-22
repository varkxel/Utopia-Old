using UnityEngine;
using Utopia.Noise;

namespace Utopia.World
{
	[CreateAssetMenu(menuName = Generator.AssetPath + "Noise Map", fileName = "Noise Map")]
	public class NoiseMap : ScriptableObject
	{
		public SimplexFractal2D.Settings settings = SimplexFractal2D.Settings.Default();
	}
}