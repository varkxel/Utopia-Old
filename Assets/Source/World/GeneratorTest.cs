using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

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

			for(int i = 0; i < generator.maskData.Length; i++)
			{
				if(generator.maskData[i] > 0.0f) Debug.Log("Yes");
				else Debug.Log("No");
			}
			
			//Profiler.BeginSample("Chunk Gen");
			//generator.GenerateChunk(new int2(0, 0));
			//Profiler.EndSample();

			done = true;
		}
	}
}