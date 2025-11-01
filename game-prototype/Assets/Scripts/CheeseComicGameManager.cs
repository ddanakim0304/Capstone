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
    public Transform cut4_LineArtBubble; // The new static bubble for Cut 4

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

    private Vector3 cut1_Bubble_OriginalScale;
    private Vector3 cut2_Bubble_OriginalScale;
    private Vector3 cut3_Panel_OriginalScale;
    private Vector3 cut4_Bubble_OriginalScale; // New variable for Cut 4's bubble

    void Start()
    {
        InitializeScene();
        StartCoroutine(PlayComicSequence());
    }

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

    private IEnumerator PlayComicSequence()
    {
        // --- CUT 1 ---
        StartCoroutine(FadeIn(cut1_LineArt, artFadeInDuration));
        yield return StartCoroutine(FadeIn(cut1_Color, artFadeInDuration));
        yield return new WaitForSeconds(delayBeforeBubble);
        yield return StartCoroutine(AnimateBubble(cut1_LineArtBubble, cut1_Bubble_OriginalScale));
        yield return new WaitForSeconds(delayBetweenCuts);

        // --- CUT 2 ---
        StartCoroutine(FadeIn(cut2_LineArt, artFadeInDuration));
        StartCoroutine(FadeIn(cut2_Color, artFadeInDuration));
        yield return StartCoroutine(FadeIn(cut2_CheeseColor, artFadeInDuration));
        yield return new WaitForSeconds(delayBeforeBubble);
        yield return StartCoroutine(AnimateBubble(cut2_LineArtBubble, cut2_Bubble_OriginalScale));
        yield return new WaitForSeconds(delayBetweenCuts);

        // --- CUT 3 ---
        yield return StartCoroutine(AnimatePanelAppearance(cut3_PanelTransform, cut3_Panel_OriginalScale));
        yield return new WaitForSeconds(delayBetweenCuts);

        // --- CUT 4 ---
        StartCoroutine(FadeIn(cut4_LineArt, artFadeInDuration));
        StartCoroutine(FadeIn(cut4_Color, artFadeInDuration));
        yield return StartCoroutine(FadeIn(cut4_ColorCheese, artFadeInDuration));
        yield return new WaitForSeconds(delayBeforeBubble);
        yield return StartCoroutine(AnimateBouncyBubble(cut4_LineArtBubble, cut4_Bubble_OriginalScale));

        // --- FINISH ---
        WinGame();
    }

    // --- Helper Functions and Coroutines ---

    private IEnumerator AnimateBouncyBubble(Transform target, Vector3 finalScale)
    {
        if (target == null) yield break;

        // Part 1: The "Boing" (Scale animation)
        yield return StartCoroutine(AnimateBubble(target, finalScale));

        // Part 2: The Shake (Position animation)
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

        // Reset to perfect position after shaking.
        target.localPosition = originalPosition;
    }

    private void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null) return;
        Color c = renderer.color;
        c.a = alpha;
        renderer.color = c;
    }

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
        
        StartCoroutine(FadeIn(target.GetComponent<SpriteRenderer>(), bubbleAnimationDuration));
        yield return StartCoroutine(AnimateScalePulse(target, Vector3.zero, finalScale, bubbleAnimationDuration));
    }

    // New function specifically for Cut 3's panel appearance.
    private IEnumerator AnimatePanelAppearance(Transform target, Vector3 finalScale)
    {
        if (target == null) yield break;
        target.gameObject.SetActive(true);
        target.localScale = Vector3.zero;
        yield return StartCoroutine(AnimateScalePulse(target, Vector3.zero, finalScale, bubbleAnimationDuration));
    }

    private IEnumerator AnimateScalePulse(Transform target, Vector3 startScale, Vector3 finalScale, float duration)
    {
        if (target == null) yield break;
        
        Vector3 overshootScale = finalScale * (1 + pulseMagnitude);
        Vector3 squashScale = finalScale * (1 - pulseMagnitude * 0.5f);
        float stepDuration = duration / 3.0f;

        yield return AnimateScale(target, startScale, overshootScale, stepDuration);
        yield return AnimateScale(target, overshootScale, squashScale, stepDuration);
        yield return AnimateScale(target, squashScale, finalScale, stepDuration);
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
}