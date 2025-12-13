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
    public float delayAfterGoodCut = 2.0f;
    public float delayAfterBadCut = 1.5f;

    private ControllerInput p1_controller;
    // Controls the game state; false when the game is paused after a cut.
    private bool canCut = true;
    private Vector3 knifeTargetPosition;

    void Start()
    {
        // Set controller reference
        if (HardwareManager.Instance != null)
        {
            p1_controller = HardwareManager.Instance.GetController(0);
        }
        
        // Ensure result sprites are hidden at the start
        if(goodResultSprite) goodResultSprite.SetActive(false);
        if(badResultSprite) badResultSprite.SetActive(false);

        // Set the knife to its starting position and give it its first target.
        if(knifeTransform != null)
        {
            knifeTransform.position = new Vector3(startX, knifeTransform.position.y, knifeTransform.position.z);
            knifeTargetPosition = new Vector3(endX, knifeTransform.position.y, knifeTransform.position.z);
        }
    }

    void Update()
    {
        if (!canCut) return;

        MoveKnife();

        // Check for player input to trigger the cut.
        if (p1_controller != null && p1_controller.IsButtonPressed)
        {
            PerformCut();
        }
    }

    // Handles the automatic back-and-forth movement of the knife.
    void MoveKnife()
    {
        if (knifeTransform == null) return;
        knifeTransform.position = Vector3.MoveTowards(knifeTransform.position, knifeTargetPosition, moveSpeed * Time.deltaTime);

        // Once the knife reaches its target, flip the target to the other side.
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
        // Lock input and stop the knife's movement immediately.
        canCut = false;

        float knifeX = knifeTransform.position.x;

        // Check if the knife's position is within the winning zone.
        if (knifeX >= goodZoneStartX && knifeX <= goodZoneEndX)
        {
            // The cut was successful.
            Debug.Log("Good Cut!");
            if(goodResultSprite) StartCoroutine(AnimateResult(goodResultSprite));
            StartCoroutine(DelayedWin());
        }
        else
        {
            // The cut was outside the good zone.
            Debug.Log("Bad Cut!");
            if(badResultSprite) StartCoroutine(AnimateResult(badResultSprite));
            StartCoroutine(RestartLevel());
        }
    }

    // Handles the failure case, resetting the game to its initial state.
    private IEnumerator RestartLevel()
    {
        // Show the 'Bad' result sprite for a moment before resetting.
        yield return new WaitForSeconds(delayAfterBadCut);

        // Hide the sprite again for the next attempt.
        if (badResultSprite) badResultSprite.SetActive(false);

        // Reset the knife's position and target.
        if (knifeTransform != null)
        {
            knifeTransform.position = new Vector3(startX, knifeTransform.position.y, knifeTransform.position.z);
            knifeTargetPosition.x = endX;
        }

        // Re-enable player input and knife movement for the next try.
        canCut = true;
    }

    // Animates the result sprite popping into view with a bouncy effect.
    private IEnumerator AnimateResult(GameObject resultObject)
    {
        resultObject.SetActive(true);
        Transform targetTransform = resultObject.transform;
        
        Vector3 originalScale = targetTransform.localScale;
        targetTransform.localScale = Vector3.zero;

        // Animate through a three-step pulse: overshoot, squash, and settle.
        Vector3 overshootScale = originalScale * (1 + resultPulseMagnitude);
        Vector3 squashScale = originalScale * (1 - resultPulseMagnitude * 0.5f);
        float stepDuration = resultPulseDuration / 3.0f;

        yield return AnimateScale(targetTransform, Vector3.zero, overshootScale, stepDuration);
        yield return AnimateScale(targetTransform, overshootScale, squashScale, stepDuration);
        yield return AnimateScale(targetTransform, squashScale, originalScale, stepDuration);
    }

    // helper to animate a transform's scale from a start to an end value.
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

    // Waits for a short delay after a good cut before winning the game.
    private IEnumerator DelayedWin()
    {
        yield return new WaitForSeconds(delayAfterGoodCut);
        WinGame();
    }
}