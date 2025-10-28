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

    private Vector3 originalScale;
    private Coroutine runningReaction;

    void Start()
    {
        originalScale = transform.localScale;
        if (normalVisuals) normalVisuals.SetActive(true);
        if (surprisedVisuals) surprisedVisuals.SetActive(false);
    }

    public void TriggerReaction(Vector3 bounceDirection)
    {
        if (runningReaction != null)
        {
            StopCoroutine(runningReaction);
        }
        runningReaction = StartCoroutine(ReactionSequence(bounceDirection));
    }

    private IEnumerator ReactionSequence(Vector3 bounceDirection)
    {
        if (normalVisuals) normalVisuals.SetActive(false);
        if (surprisedVisuals) surprisedVisuals.SetActive(true);

        Vector3 startPos = transform.position;
        Vector3 bounceTarget = startPos + bounceDirection * bounceDistance;
        float timer = 0f;

        while (timer < bounceDuration)
        {
            transform.position = Vector3.Lerp(startPos, bounceTarget, timer / bounceDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = bounceTarget;

        float stepDuration = shudderDuration / 4.0f;
        Vector3 squashScale = new Vector3(originalScale.x * (1 + shudderMagnitude), originalScale.y * (1 - shudderMagnitude), originalScale.z);
        
        yield return AnimateScale(originalScale, squashScale, stepDuration);
        yield return AnimateScale(squashScale, originalScale, stepDuration);
        yield return AnimateScale(originalScale, squashScale, stepDuration);
        yield return AnimateScale(squashScale, originalScale, stepDuration);

        runningReaction = null;
    }

    private IEnumerator AnimateScale(Vector3 startScale, Vector3 endScale, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = endScale;
    }
}