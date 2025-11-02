using UnityEngine;
using System.Collections;

public class CheeseComicGameManager : MiniGameManager
{
    [Header("Cut 1 Elements")]
    public SpriteRenderer cut1_LineArt;
    public SpriteRenderer cut1_Color;
    public Transform cut1_LineArtBubble;

    [Header("Cut 2 Elements")]
    public SpriteRenderer cut2_LineArt;
    public SpriteRenderer cut2_Color;
    public SpriteRenderer cut2_CheeseColor;
    public Transform cut2_LineArtBubble;

    [Header("Cut 3 Elements")]
    public Transform cut3_PanelTransform;
    public SpriteRenderer cut3_LineArt;
    public SpriteRenderer cut3_Color;

    [Header("Cut 4 Elements")]
    public SpriteRenderer cut4_LineArt;
    public SpriteRenderer cut4_Color;
    public SpriteRenderer cut4_ColorCheese;
    public Transform cut4_LineArtBubble;

    [Header("Animation Timings")]
    public float artFadeInDuration = 1.0f;
    public float delayBeforeBubble = 1.0f;
    public float bubbleAnimationDuration = 0.5f;
    public float delayBetweenCuts = 2.0f;

    [Header("Animation Settings")]
    public float pulseMagnitude = 0.1f;
    
    [Header("Bouncy Shake Animation (For Cut 4)")]
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.05f;

    // Cache the original scales of animated objects
    private Vector3 cut1_Bubble_OriginalScale;
    private Vector3 cut2_Bubble_OriginalScale;
    private Vector3 cut3_Panel_OriginalScale;
    private Vector3 cut4_Bubble_OriginalScale;

    void Start()
    {
        // Prepare the scene and then start the main animation sequence.
        InitializeScene();
        StartCoroutine(PlayComicSequence());
    }

    // Sets all comic elements to their initial state and caches scales.
    void InitializeScene()
    {
        SetAlpha(cut1_LineArt, 0);
        SetAlpha(cut1_Color, 0);
        if (cut1_LineArtBubble != null) { cut1_Bubble_OriginalScale = cut1_LineArtBubble.localScale; cut1_LineArtBubble.gameObject.SetActive(false); }
        
        SetAlpha(cut2_LineArt, 0);
        SetAlpha(cut2_Color, 0);
        SetAlpha(cut2_CheeseColor, 0);
        if (cut2_LineArtBubble != null) { cut2_Bubble_OriginalScale = cut2_LineArtBubble.localScale; cut2_LineArtBubble.gameObject.SetActive(false); }
        
        if (cut3_PanelTransform != null) { cut3_Panel_OriginalScale = cut3_PanelTransform.localScale; cut3_PanelTransform.gameObject.SetActive(false); }
        
        SetAlpha(cut4_LineArt, 0);
        SetAlpha(cut4_Color, 0);
        SetAlpha(cut4_ColorCheese, 0);
        if (cut4_LineArtBubble != null) { cut4_Bubble_OriginalScale = cut4_LineArtBubble.localScale; cut4_LineArtBubble.gameObject.SetActive(false); }
    }

    // This coroutine controls the timing and animation
    private IEnumerator PlayComicSequence()
    {
        // Panel 1: A simple fade-in and bubble appearance.
        StartCoroutine(FadeIn(cut1_LineArt, artFadeInDuration));
        yield return StartCoroutine(FadeIn(cut1_Color, artFadeInDuration));
        yield return new WaitForSeconds(delayBeforeBubble);
        yield return StartCoroutine(AnimateBubble(cut1_LineArtBubble, cut1_Bubble_OriginalScale));
        yield return new WaitForSeconds(delayBetweenCuts);

        // Panel 2: Similar to the first panel.
        StartCoroutine(FadeIn(cut2_LineArt, artFadeInDuration));
        StartCoroutine(FadeIn(cut2_Color, artFadeInDuration));
        yield return StartCoroutine(FadeIn(cut2_CheeseColor, artFadeInDuration));
        yield return new WaitForSeconds(delayBeforeBubble);
        yield return StartCoroutine(AnimateBubble(cut2_LineArtBubble, cut2_Bubble_OriginalScale));
        yield return new WaitForSeconds(delayBetweenCuts);

        // Panel 3: The whole panel pops into view with a bouncy effect.
        yield return StartCoroutine(AnimatePanelAppearance(cut3_PanelTransform, cut3_Panel_OriginalScale));
        yield return new WaitForSeconds(delayBetweenCuts);

        // Panel 4: Final panel with a unique bouncy shake animation on the bubble.
        StartCoroutine(FadeIn(cut4_LineArt, artFadeInDuration));
        StartCoroutine(FadeIn(cut4_Color, artFadeInDuration));
        yield return StartCoroutine(FadeIn(cut4_ColorCheese, artFadeInDuration));
        yield return new WaitForSeconds(delayBeforeBubble);
        yield return StartCoroutine(AnimateBouncyBubble(cut4_LineArtBubble, cut4_Bubble_OriginalScale));

        // Once the sequence is complete, the minigame is won (next scene)
        WinGame();
    }
    
    // A special bubble animation that combines a scale pulse with a random shake
    private IEnumerator AnimateBouncyBubble(Transform target, Vector3 finalScale)
    {
        if (target == null) yield break;

        // First, make the bubble "boing" into view with a scaling animation.
        yield return StartCoroutine(AnimateBubble(target, finalScale));

        // Then, add a random positional shake for emphasis.
        Vector3 originalPosition = target.localPosition;
        float timer = 0f;
        while (timer < shakeDuration)
        {
            float xOffset = Random.Range(-1f, 1f) * shakeMagnitude;
            float yOffset = Random.Range(-1f, 1f) * shakeMagnitude;
            target.localPosition = originalPosition + new Vector3(xOffset, yOffset, 0);
            
            timer += Time.deltaTime;
            yield return null;
        }

        // Snap back to the original position to clean up any offsets from the shake.
        target.localPosition = originalPosition;
    }

    // A utility function to set the alpha on a SpriteRenderer's color.
    private void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null) return;
        Color c = renderer.color;
        c.a = alpha;
        renderer.color = c;
    }

    // Coroutine to animate a SpriteRenderer's alpha from 0 to 1 over a set duration.
    private IEnumerator FadeIn(SpriteRenderer renderer, float duration)
    {
        if (renderer == null) yield break;
        float timer = 0f;
        while (timer < duration)
        {
            SetAlpha(renderer, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        SetAlpha(renderer, 1f);
    }

    private IEnumerator AnimateBubble(Transform target, Vector3 finalScale)
    {
        if (target == null) yield break;

        target.gameObject.SetActive(true);
        target.localScale = Vector3.zero;

        // Fade in the bubble sprite while scaling it up with a pulse effect
        StartCoroutine(FadeIn(target.GetComponent<SpriteRenderer>(), bubbleAnimationDuration));
        yield return StartCoroutine(AnimateScalePulse(target, Vector3.zero, finalScale, bubbleAnimationDuration));
    }

    // A simplified appearance animation for Cut 3
    private IEnumerator AnimatePanelAppearance(Transform target, Vector3 finalScale)
    {
        if (target == null) yield break;
        target.gameObject.SetActive(true);
        target.localScale = Vector3.zero;
        yield return StartCoroutine(AnimateScalePulse(target, Vector3.zero, finalScale, bubbleAnimationDuration));
    }

    // Creates a bouncy "boing" effect
    private IEnumerator AnimateScalePulse(Transform target, Vector3 startScale, Vector3 finalScale, float duration)
    {
        if (target == null) yield break;
        
        Vector3 overshootScale = finalScale * (1 + pulseMagnitude);
        Vector3 squashScale = finalScale * (1 - pulseMagnitude * 0.5f);
        float stepDuration = duration / 3.0f;

        // Animate through the three steps: overshoot, squash, and settle.
        yield return AnimateScale(target, startScale, overshootScale, stepDuration);
        yield return AnimateScale(target, overshootScale, squashScale, stepDuration);
        yield return AnimateScale(target, squashScale, finalScale, stepDuration);
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
}