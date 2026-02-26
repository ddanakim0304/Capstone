using UnityEngine;
public class CarCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The car transform to follow.")]
    public Transform car;

    [Header("Follow Settings")]
    [Tooltip("How quickly the camera catches up. 1 = instant, 0.01 = very slow.")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.15f;

    [Tooltip("Horizontal look-ahead offset (positive = camera leads ahead of the car).")]
    public float lookAheadX = 2f;

    [Header("Axis Lock")]
    [Tooltip("Lock the Y axis so the camera never moves up/down.")]
    public bool lockY = true;

    [Tooltip("Lock the Z axis (leave enabled for 2-D games).")]
    public bool lockZ = true;

    [Header("Bounds (World X) – optional")]
    [Tooltip("Prevent the camera from scrolling before this world X. 0 = disabled.")]
    public float minCameraX = 0f;
    [Tooltip("Prevent the camera from scrolling past this world X. 0 = disabled.")]
    public float maxCameraX = 0f;   // 0 means 'no cap'

    // ─────────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (car == null) return;

        // Desired position: follow car's X with the look-ahead offset
        float desiredX = car.position.x + lookAheadX;
        float desiredY = lockY ? transform.position.y : car.position.y;
        float desiredZ = lockZ ? transform.position.z : car.position.z;

        Vector3 desired = new Vector3(desiredX, desiredY, desiredZ);

        // Smooth lerp
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed);

        // Optional: clamp to road bounds
        if (maxCameraX > minCameraX)   // both set and meaningful
        {
            smoothed.x = Mathf.Clamp(smoothed.x, minCameraX, maxCameraX);
        }

        transform.position = smoothed;
    }
}
