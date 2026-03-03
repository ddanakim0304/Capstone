using UnityEngine;
using System.Collections;

public class CarCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform car;

    [Header("Follow Settings")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.15f;
    public float lookAheadX = 2f;

    [Header("Axis Lock")]
    public bool lockY = true;
    public bool lockZ = true;

    [Header("Bounds (World X)")]
    // 0 means no limit
    public float minCameraX = 0f;
    public float maxCameraX = 0f;  

    [Header("Look-Ahead Lerp (on arrival)")]
    public float arrivalLookAheadX = 6f;
    public float arrivalLookAheadDuration = 1f;

    [Header("House Focus")]
    public Vector3 houseFocusPosition;
    public float houseFocusMoveDuration = 1f;

    private bool isFocusing = false;

    // Trigger the camera sequence to focus on the house
    public void TriggerHouseFocus()
    {
        StartCoroutine(FocusOnHouse());
    }

    // Coroutine to smoothly move the camera to the target house position
    private System.Collections.IEnumerator FocusOnHouse()
    {
        isFocusing = true;
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        float duration = Mathf.Max(houseFocusMoveDuration, 0.01f);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, houseFocusPosition, elapsed / duration);
            yield return null;
        }
        transform.position = houseFocusPosition;
    }

    public void TriggerArrivalLookAhead()
    {
        StartCoroutine(LerpLookAhead(lookAheadX, arrivalLookAheadX, arrivalLookAheadDuration));
    }

    // Adjusts the look-ahead offset over time for smooth transitions
    private System.Collections.IEnumerator LerpLookAhead(float from, float to, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(duration, 0.01f);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            lookAheadX = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        lookAheadX = to;
    }

    // Updates camera position to follow the car with offsets and limits
    void LateUpdate()
    {
        if (car == null || isFocusing) return;

        // Follow car X with offset
        float desiredX = car.position.x + lookAheadX;
        float desiredY = lockY ? transform.position.y : car.position.y;
        float desiredZ = lockZ ? transform.position.z : car.position.z;

        Vector3 desired = new Vector3(desiredX, desiredY, desiredZ);

        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed);

        if (maxCameraX > minCameraX)
        {
            smoothed.x = Mathf.Clamp(smoothed.x, minCameraX, maxCameraX);
        }

        transform.position = smoothed;
    }
}
