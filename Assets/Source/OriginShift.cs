using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Utopia
{
	[BurstCompile]
	public sealed class OriginShift : MonoBehaviour
	{
		public Transform player;

		public float shiftSize = 4096.0f;

		public int2 shift { get; private set; }

		public double2 position
		{
			get
			{
				double2 pos = shift;
				pos *= shiftSize;
				pos += ((float3) player.position).xz;
				return pos;
			}
		}

		void Update()
		{
			CalculateShift(player.position, shiftSize, out float3 shiftAmount, out int2 shiftIndex);

			// Move shift counter
			shift += shiftIndex;

			// Skip the expensive shift operation if nothing needs to be shifted.
			if(shiftIndex.x == 0 && shiftIndex.y == 0) return;

			// Shift the player & everything that is a child of this GameObject
			player.position = (float3) player.position - shiftAmount;
			for(int i = 0; i < transform.childCount; i++)
			{
				Transform child = transform.GetChild(i);
				child.position = (float3) child.position - shiftAmount;
			}
		}

		[BurstCompile]
		private static void CalculateShift(in float3 playerPos, float shiftSize, out float3 shiftAmount, out int2 shiftIndex)
		{
			// Set shift amount to the direction to shift in
			shiftAmount = playerPos;
			shiftAmount.xz /= shiftSize;
			shiftAmount.xz = math.trunc(shiftAmount.xz);

			// Increment counter in direction to track shift
			shiftIndex = (int2) shiftAmount.xz;

			// Convert to actual distance
			shiftAmount.xz *= shiftSize;
		}
	}
}