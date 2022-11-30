using Unity.Mathematics;
using UnityEngine;

namespace Utopia.World
{
	public sealed class SpawnManager : MonoBehaviour
	{
		public Transform target;
		
		public float shiftSize = 1024.0f;
		
		private void Update()
		{
			float3 targetPosition = target.position;
			
			float2 shift = targetPosition.xz;
			shift /= shiftSize;
			shift = math.trunc(shift);
			shift *= shiftSize;
			float3 shift3D = new float3(shift.x, 0.0f, shift.y);
			
			Transform t = transform;
			float3 thisPosition = t.position;
			thisPosition -= shift3D;
			t.position = thisPosition;
		}
	}
}