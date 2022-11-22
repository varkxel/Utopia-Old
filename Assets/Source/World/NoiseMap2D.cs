using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using Utopia.Noise;

namespace Utopia.World
{
	[CreateAssetMenu(menuName = Generator.AssetPath + "Noise Map", fileName = "Noise Map")]
	public class NoiseMap2D : ScriptableObject
	{
		public NativeArray<double2> octavePositions;
		
		// Serialized data
		public SimplexFractal2D.Settings settings = SimplexFractal2D.Settings.Default();
	}
}