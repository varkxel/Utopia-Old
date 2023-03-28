using UnityEngine;

public class CameraPan : MonoBehaviour
{
    public Transform target;
    public float speed = 1.0f;

    void Update()
    {
        transform.LookAt(target);
        transform.Translate(Vector3.right * Time.deltaTime * speed);
    }
}