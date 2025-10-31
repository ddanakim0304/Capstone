using UnityEngine;
using TMPro;
using System.Collections;

public class WavelengthGameManager : MiniGameManager
{
    [Header("Player References")]
    public PlayerWaveController player1Wave;
    public PlayerWaveController player2Wave;
    public Transform player1Transform;
    public Transform player2Transform;

    [Header("Player Visuals")]
    public GameObject player1NormalVisuals;
    public GameObject player1HappyVisuals;
    public GameObject player2NormalVisuals;
    public GameObject player2HappyVisuals;

    [Header("Scene Objects")]
    public GameObject noiseParent;
    
    [Header("UI References")]
    public TextMeshProUGUI matchText;

    [Header("Game Rules")]
    public float matchThreshold = 0.1f;
    public float timeToWin = 3f;
    public float delayAfterWin = 1.0f;

    [Header("Victory Animation Settings")]
    public float pulseMagnitude = 0.2f; // Renamed from bounceMagnitude
    public float pulseDuration = 0.5f;  // Renamed from bounceDuration
    
    [Header("Visual Feedback")]
    public LineRenderer p1LineRenderer;
    public LineRenderer p2LineRenderer;
    public Color matchedColor = Color.white;
    
    private float matchTimer = 0f;
    private Color p1InitialColor;
    private Color p2InitialColor;

    void Start()
    {
        if(p1LineRenderer) p1InitialColor = p1LineRenderer.startColor;
        if(p2LineRenderer) p2InitialColor = p2LineRenderer.startColor;
        if (matchText != null) matchText.gameObject.SetActive(false);

        if (player1NormalVisuals != null) player1NormalVisuals.SetActive(true);
        if (player1HappyVisuals != null) player1HappyVisuals.SetActive(false);
        if (player2NormalVisuals != null) player2NormalVisuals.SetActive(true);
        if (player2HappyVisuals != null) player2HappyVisuals.SetActive(false);
    }

    void Update()
    {
        if (isGameWon) return;
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        float frequency1 = player1Wave.Frequency;
        float frequency2 = player2Wave.Frequency;
        float frequencyDifference = Mathf.Abs(frequency1 - frequency2);

        if (frequencyDifference < matchThreshold)
        {
            matchTimer += Time.deltaTime;

            if (p1LineRenderer) p1LineRenderer.startColor = p1LineRenderer.endColor = matchedColor;
            if (p2LineRenderer) p2LineRenderer.startColor = p2LineRenderer.endColor = matchedColor;

            if (matchText != null)
            {
                matchText.gameObject.SetActive(true);
                float remainingTime = timeToWin - matchTimer;
                if (remainingTime < 0) remainingTime = 0;
                matchText.text = string.Format("Match!\n{0:F1}s", remainingTime);
            }

            if (matchTimer >= timeToWin)
            {
                StartCoroutine(WinSequence());
            }
        }
        else
        {
            matchTimer = 0f;
            if (p1LineRenderer) p1LineRenderer.startColor = p1LineRenderer.endColor = p1InitialColor;
            if (p2LineRenderer) p2LineRenderer.startColor = p2LineRenderer.endColor = p2InitialColor;
            if (matchText != null) matchText.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator WinSequence()
    {
        isGameWon = true;

        if (player1Wave != null) player1Wave.isUpdating = false;
        if (player2Wave != null) player2Wave.isUpdating = false;
        if (noiseParent != null) noiseParent.SetActive(false);

        if (player1NormalVisuals != null) player1NormalVisuals.SetActive(false);
        if (player1HappyVisuals != null) player1HappyVisuals.SetActive(true);
        if (player2NormalVisuals != null) player2NormalVisuals.SetActive(false);
        if (player2HappyVisuals != null) player2HappyVisuals.SetActive(true);

        // Start the procedural scale animations and wait for them to finish.
        StartCoroutine(AnimateBoing(player1Transform));
        yield return StartCoroutine(AnimateBoing(player2Transform));

        yield return new WaitForSeconds(delayAfterWin);
        WinGame();
    }

    // A coroutine to perform the procedural scale pulse animation for one character.
    private IEnumerator AnimateBoing(Transform targetTransform)
    {
        if (targetTransform == null) yield break;

        Vector3 originalScale = targetTransform.localScale;
        float stepDuration = pulseDuration / 3.0f;

        // Define the target scales for the squash and stretch effect.
        Vector3 overshootScale = originalScale * (1 + pulseMagnitude);
        Vector3 squashScale = originalScale * (1 - pulseMagnitude * 0.5f);

        // Perform the three animation steps: overshoot, squash, and return to normal.
        yield return AnimateScale(targetTransform, originalScale, overshootScale, stepDuration);
        yield return AnimateScale(targetTransform, overshootScale, squashScale, stepDuration);
        yield return AnimateScale(targetTransform, squashScale, originalScale, stepDuration);
    }
    
    // A helper coroutine to smoothly animate scale between two states.
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
}