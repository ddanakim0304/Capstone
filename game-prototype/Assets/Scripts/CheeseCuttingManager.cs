using UnityEngine;
using System.Collections;

public class CheeseCuttingManager : MiniGameManager
{
    [Header("Game Elements")]
    public Transform knifeTransform;
    public GameObject goodResultSprite;
    public GameObject badResultSprite;

    [Header("Automatic Movement Settings")]
    public float startX = -6.6f;
    public float endX = 1.4f;
    public float moveSpeed = 4f;

    [Header("Winning Zone")]
    public float goodZoneStartX = -2.48f;
    public float goodZoneEndX = -1.21f;

    [Header("Animation Settings")]
    public float resultPulseMagnitude = 0.1f;
    public float resultPulseDuration = 0.3f;
    public float delayAfterGoodCut = 2.0f; // Renamed for clarity
    public float delayAfterBadCut = 1.5f;  // New variable for the fail state

    private ControllerInput p1_controller;
    private bool canCut = true; // Changed from hasCut to allow resetting
    private Vector3 knifeTargetPosition;

    void Start()
    {
        if (HardwareManager.Instance != null)
        {
            p1_controller = HardwareManager.Instance.GetController(0);
        }
        
        if(goodResultSprite) goodResultSprite.SetActive(false);
        if(badResultSprite) badResultSprite.SetActive(false);

        if(knifeTransform != null)
        {
            knifeTransform.position = new Vector3(startX, knifeTransform.position.y, knifeTransform.position.z);
            knifeTargetPosition = new Vector3(endX, knifeTransform.position.y, knifeTransform.position.z);
        }
    }

    void Update()
    {
        // We only allow updates if the player is allowed to cut.
        if (!canCut) return;

        MoveKnife();

        if (p1_controller != null && p1_controller.IsButtonPressed)
        {
            PerformCut();
        }
    }

    void MoveKnife()
    {
        if (knifeTransform == null) return;
        knifeTransform.position = Vector3.MoveTowards(knifeTransform.position, knifeTargetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(knifeTransform.position, knifeTargetPosition) < 0.01f)
        {
            if (knifeTargetPosition.x == endX)
            {
                knifeTargetPosition.x = startX;
            }
            else
            {
                knifeTargetPosition.x = endX;
            }
        }
    }

    void PerformCut()
    {
        canCut = false; // Immediately disable further input and movement

        float knifeX = knifeTransform.position.x;

        // Check if the cut was good or bad
        if (knifeX >= goodZoneStartX && knifeX <= goodZoneEndX)
        {
            // --- GOOD CUT PATH ---
            Debug.Log("Good Cut!");
            if(goodResultSprite) StartCoroutine(AnimateResult(goodResultSprite));
            StartCoroutine(DelayedWin()); // Proceed to the next level
        }
        else
        {
            // --- BAD CUT PATH ---
            Debug.Log("Bad Cut!");
            if(badResultSprite) StartCoroutine(AnimateResult(badResultSprite));
            StartCoroutine(RestartLevel()); // Restart this level
        }
    }

    private IEnumerator RestartLevel()
    {
        // Wait for a moment to show the "Bad" sprite.
        yield return new WaitForSeconds(delayAfterBadCut);

        // Hide the bad sprite again.
        if (badResultSprite) badResultSprite.SetActive(false);

        // Reset the knife's position.
        if (knifeTransform != null)
        {
            knifeTransform.position = new Vector3(startX, knifeTransform.position.y, knifeTransform.position.z);
            knifeTargetPosition.x = endX;
        }

        // Allow the player to cut again.
        canCut = true;
    }

    private IEnumerator AnimateResult(GameObject resultObject)
    {
        resultObject.SetActive(true);
        Transform targetTransform = resultObject.transform;
        
        Vector3 originalScale = targetTransform.localScale;
        targetTransform.localScale = Vector3.zero;

        Vector3 overshootScale = originalScale * (1 + resultPulseMagnitude);
        Vector3 squashScale = originalScale * (1 - resultPulseMagnitude * 0.5f);
        float stepDuration = resultPulseDuration / 3.0f;

        yield return AnimateScale(targetTransform, Vector3.zero, overshootScale, stepDuration);
        yield return AnimateScale(targetTransform, overshootScale, squashScale, stepDuration);
        yield return AnimateScale(targetTransform, squashScale, originalScale, stepDuration);
    }

    private IEnumerator AnimateScale(Transform target, Vector3 start, Vector3 end, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            target.localScale = Vector3.Lerp(start, end, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        target.localScale = end;
    }

    private IEnumerator DelayedWin()
    {
        yield return new WaitForSeconds(delayAfterGoodCut);
        WinGame();
    }
}