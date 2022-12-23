using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utopia
{
	[BurstCompile]
	public sealed class OriginShift : MonoBehaviour
	{
		/// <summary>
		/// The player's transform to keep track of.
		/// </summary>
		[Tooltip("The player's transform to keep track of.")]
		public Transform player;

		/// <summary>
		/// The distance the player has to travel before shifting the origin.
		/// </summary>
		[Tooltip("The distance the player has to travel before shifting the origin.")]
		public float shiftSize = 2048.0f;

		public int2 shift { get; private set; }

		/// <summary>
		/// Gets the current position of the player as a <see cref="double2"/>.
		/// </summary>
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

		private void Update()
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

		/// <summary>
		/// Calculate whether the player has to shift or not.
		/// </summary>
		/// <param name="playerPos">The current player position tracked by <see cref="player"/>.</param>
		/// <param name="shiftSize">The amount to shift the world by when the value is reached.</param>
		/// <param name="shiftAmount">The amount the world has been shifted this frame.</param>
		/// <param name="shiftIndex">The amount the world has been shifted this frame in steps.</param>
		[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
		private static void CalculateShift(in float3 playerPos, float shiftSize, out float3 shiftAmount, out int2 shiftIndex)
		{
			// Set shift amount to the direction to shift in
			shiftAmount = playerPos;
			shiftAmount.xz /= shiftSize;
			shiftAmount.xz = trunc(shiftAmount.xz);

			// Increment counter in direction to track shift
			shiftIndex = (int2) shiftAmount.xz;

			// Convert to actual distance
			shiftAmount.xz *= shiftSize;
		}
	}
}