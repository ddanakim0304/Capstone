using UnityEngine;
using System.Collections;

public class BumpReaction : MonoBehaviour
{
    [Header("Visual Groups")]
    [Tooltip("The GameObject that holds all the 'normal' sprites.")]
    public GameObject normalVisuals;
    [Tooltip("The GameObject that holds all the 'surprised' sprites.")]
    public GameObject surprisedVisuals;

    [Header("Animation Settings")]
    public float bounceDistance = 0.5f;
    public float bounceDuration = 0.1f;
    public float shudderMagnitude = 0.15f;
    public float shudderDuration = 0.4f;

    // Stores the initial scale to return to after the animation finishes.
    private Vector3 originalScale;
    // A reference to the running coroutine to prevent multiple reactions from overlapping.
    private Coroutine runningReaction;

    void Start()
    {
        // Cache the starting scale and set the initial visual state.
        originalScale = transform.localScale;
        if (normalVisuals) normalVisuals.SetActive(true);
        if (surprisedVisuals) surprisedVisuals.SetActive(false);
    }

    public void TriggerReaction(Vector3 bounceDirection)
    {
        // If a reaction is already playing, stop it before starting a new one.
        if (runningReaction != null)
        {
            StopCoroutine(runningReaction);
        }
        // Start the animation sequence.
        runningReaction = StartCoroutine(ReactionSequence(bounceDirection));
    }

    private IEnumerator ReactionSequence(Vector3 bounceDirection)
    {
        // Swap to the 'surprised' visuals for the duration of the reaction.
        if (normalVisuals) normalVisuals.SetActive(false);
        if (surprisedVisuals) surprisedVisuals.SetActive(true);

        // Animate the initial knockback.
        Vector3 startPos = transform.position;
        Vector3 bounceTarget = startPos + bounceDirection * bounceDistance;
        float timer = 0f;

        // Lerp the position to the bounce target over the specified duration.
        while (timer < bounceDuration)
        {
            transform.position = Vector3.Lerp(startPos, bounceTarget, timer / bounceDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = bounceTarget;

        // Animate a shudder effect
        float stepDuration = shudderDuration / 4.0f;
        Vector3 squashScale = new Vector3(originalScale.x * (1 + shudderMagnitude), originalScale.y * (1 - shudderMagnitude), originalScale.z);
        yield return AnimateScale(originalScale, squashScale, stepDuration);
        yield return AnimateScale(squashScale, originalScale, stepDuration);
        yield return AnimateScale(originalScale, squashScale, stepDuration);
        yield return AnimateScale(squashScale, originalScale, stepDuration);

        // Clear the coroutine reference now that the animation is complete.
        runningReaction = null;
    }

    // A helper coroutine to animate the scale of this object over a set duration.
    private IEnumerator AnimateScale(Vector3 startScale, Vector3 endScale, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        // Snap to the final scale to ensure the animation ends precisely.
        transform.localScale = endScale;
    }
}