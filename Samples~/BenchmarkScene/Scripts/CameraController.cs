using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 1;
    private Vector3 startPosition;
    private float zoomFactor = 0;

    private void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        zoomFactor += Input.mouseScrollDelta.y * sensitivity;
        transform.position = startPosition + (transform.forward * zoomFactor);
    }
}