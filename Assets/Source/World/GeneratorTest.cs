using UnityEngine;
using UnityEngine.Profiling;
using Unity.Mathematics;

namespace Utopia.World
{
	[RequireComponent(typeof(Generator))]
	public class GeneratorTest : MonoBehaviour
	{
		private Generator generator;
		
		void Start()
		{
			generator = GetComponent<Generator>();
			
			Profiler.BeginSample("Mask Gen");
			generator.GenerateMask();
			Profiler.EndSample();
		}

		private bool done = false;
		void Update()
		{
			if(!generator.isMaskGenerated || done) return;
			
			Profiler.BeginSample("Chunk Gen");
			generator.GenerateChunk(new int2(1, 2));
			Profiler.EndSample();

			done = true;
		}
	}
}