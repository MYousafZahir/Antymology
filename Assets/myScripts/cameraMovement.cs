using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class cameraMovement : MonoBehaviour
{
    // camera
    private Camera cam;
    private float cameraSpeed = 5.0f;
    private float sprintCameraSpeed = 20.0f;
    void Start() {
        cam = Camera.main;
    }

    void Update() {
        Vector3 moveDirection = Vector3.zero; // Initialize movement direction
        
        if (Input.GetKey(KeyCode.LeftShift)) cameraSpeed = sprintCameraSpeed; // Sprint
        else cameraSpeed = 5.0f; // Walk

        // Movement controls
        if (Input.GetKey(KeyCode.Q)) moveDirection += Vector3.down;
        if (Input.GetKey(KeyCode.E)) moveDirection += Vector3.up;
        if (Input.GetKey(KeyCode.W)) moveDirection += cam.transform.forward;
        if (Input.GetKey(KeyCode.S)) moveDirection -= cam.transform.forward;
        if (Input.GetKey(KeyCode.A)) moveDirection -= cam.transform.right;
        if (Input.GetKey(KeyCode.D)) moveDirection += cam.transform.right;

        // Apply movement
        cam.transform.position += moveDirection * Time.deltaTime * cameraSpeed;

        // Camera rotation
        if (Input.GetMouseButton(1)) {
            float rotationX = Input.GetAxis("Mouse X") * 5;
            float rotationY = Input.GetAxis("Mouse Y") * 5;

            // Apply yaw (left and right rotation)
            cam.transform.Rotate(Vector3.up, rotationX, Space.World);

            // Apply pitch (up and down rotation), avoiding roll
            Vector3 cameraRotation = cam.transform.localEulerAngles;
            float newPitch = cameraRotation.x - rotationY;
            // Optional: Clamp newPitch to limit pitch range
            cam.transform.localEulerAngles = new Vector3(newPitch, cameraRotation.y, 0); // Ensures no roll
        }
    }


}
