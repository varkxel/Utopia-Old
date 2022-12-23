using UnityEngine;

namespace Utopia
{
	/// <summary>
	/// Basic camera controller to allow panning through the scene.
	/// </summary>
	public class CameraController : MonoBehaviour
	{
		private Transform t;

		public float acceleration = 4.0f;
		private float speed = 1.0f;

		private void Awake()
		{
			t = transform;
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		
		private void Update()
		{
			t.Rotate(new Vector3(0, 1, 0), Input.GetAxisRaw("Mouse X"), Space.World);
			t.Rotate(new Vector3(1, 0, 0), -Input.GetAxisRaw("Mouse Y"), Space.Self);
			
			/*
			 * Not pretty code.
			 * This is just thrown together as a temporary measure.
			 */
			
			Vector3 movement = new Vector3
			(
				Input.GetAxisRaw("Horizontal"),
				0.0f,
				Input.GetAxisRaw("Vertical")
			);
			if(Input.GetKey(KeyCode.E)) movement.y += 1.0f;
			else if(Input.GetKey(KeyCode.Q)) movement.y -= 1.0f;
			
			bool isMoving = movement.magnitude > 0.0f;
			
			movement = movement.normalized;
			movement = t.TransformDirection(movement);
			t.position += movement * (speed * Time.deltaTime);

			if(isMoving) speed += Time.deltaTime * acceleration;
			else speed = 1.0f;
		}
	}
}