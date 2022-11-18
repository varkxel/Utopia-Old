using UnityEngine;
using Utopia.Noise;

namespace Utopia.World.Biomes
{
	[CreateAssetMenu(menuName = "Utopia/Biomes/Noise Map", fileName = "Biome Noise Map", order = 100)]
	public class BiomeNoiseMap : ScriptableObject
	{
		public SimplexFractal2D.Settings settings = SimplexFractal2D.Settings.Default();
	}
}