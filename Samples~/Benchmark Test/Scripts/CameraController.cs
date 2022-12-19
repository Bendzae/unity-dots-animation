using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BenchmarkScene.Scripts
{
    public class CameraController : MonoBehaviour
    {
        // Movement based Scroll Wheel Zoom.
        public Transform parentObject;
        public float zoomLevel;
        public float sensitivity=1;
        public float speed = 30;
        public float maxZoom=30;
        float zoomPosition;
        void Update()
        {
            zoomLevel += Input.mouseScrollDelta.y * sensitivity;
            zoomLevel = Mathf.Clamp(zoomLevel, 0, maxZoom);
            zoomPosition = Mathf.MoveTowards(zoomPosition, zoomLevel, speed * Time.deltaTime);
            transform.position = parentObject.position + (transform.forward * zoomPosition);
        }
    }
}
