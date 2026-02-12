using UnityEngine;

public class TVCameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform slime1;
    public Transform slime2;

    [Header("Settings")]
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    // Lock Y if you only want horizontal scrolling
    public bool lockYAxis = true;

    void FixedUpdate()
    {
        if (slime1 == null || slime2 == null) return;

        // 1. Find the center point between the two slimes
        Vector3 centerPoint = (slime1.position + slime2.position) / 2;

        // 2. Calculate desired position
        Vector3 desiredPosition = centerPoint + offset;

        if (lockYAxis)
        {
            desiredPosition.y = transform.position.y;
        }

        // 3. Smoothly move the camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}