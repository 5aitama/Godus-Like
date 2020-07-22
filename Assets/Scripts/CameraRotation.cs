using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotation : MonoBehaviour
{

    public float cameraDistance = 30;
    public bool autoRotate = false;
    public float rotationSpeed = 20f;
    private Transform cameraTransform;

    private void Awake()
    {
        cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if(autoRotate)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        cameraTransform.position = transform.rotation * Vector3.back * cameraDistance + transform.position;
        cameraTransform.rotation = Quaternion.LookRotation((transform.position - cameraTransform.position).normalized);
    }
}
