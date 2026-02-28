using UnityEngine;
using System.Collections;
public class FinalCutsceneMiniGame : MiniGameManager
{
    public static FinalCutsceneMiniGame Instance { get; private set; }

    [System.Serializable]
    public class AnimationEntry
    {
        [Tooltip("Root GameObject that holds all child SpriteRenderers for this animation.")]
        public GameObject animObject;

        [Tooltip("Optional reference – not used for fading (kept for inspector convenience).")]
        public SpriteRenderer paperSprite;

        [Tooltip("Seconds over which all child sprites fade in (0 → 1).")]
        public float fadeInDuration = 0.5f;

        [Tooltip("Seconds this animation stays fully visible before fading out.")]
        public float displayDuration = 2f;

        [Tooltip("Seconds over which all child sprites fade out (1 → 0).")]
        public float fadeDuration = 0.5f;

        [Tooltip("Seconds to wait (fully transparent) between this entry and the next.")]
        public float gapAfter = 0f;
    }

    [Header("Cutscene Timing")]
    [Tooltip("Seconds to wait after TriggerCutscene() is called before the first animation starts.")]
    public float cutsceneStartDelay = 1f;

    [Header("Animation Sequence")]
    [Tooltip("Animation objects played in order. All should start DISABLED in the scene.")]
    public AnimationEntry[] animations;

    [Header("House Sprites")]
    [Tooltip("The original closed-house sprite. Should be ACTIVE at start. " +
             "Fades OUT when the last animation fades in, fades back IN when it fades out.")]
    public GameObject houseClosed;

    [Tooltip("The opened-house sprite shown during the last animation. Should be INACTIVE at start. " +
             "Fades IN with the last animation, fades back OUT after it ends.")]
    public GameObject houseOpened;

    [Header("House Window")]
    [Tooltip("Child of the house that represents the lit window; fades IN at the end.")]
    public GameObject windowLight;

    [Tooltip("Child of the house that represents the dark window; fades OUT at the end.")]
    public GameObject windowDark;

    [Tooltip("Seconds over which windowDark fades out.")]
    public float houseFadeOutDuration = 0.5f;

    [Tooltip("Seconds over which windowLight fades in.")]
    public float houseFadeInDuration  = 0.5f;

    [Tooltip("Seconds to wait after the house cross-fade before WinGame() is called.")]
    public float houseFinishDelay     = 1f;

    [Header("Post-Cutscene: Camera Focus & Car Fade")]
    [Tooltip("Camera follow script – will be told to focus on the house.")]
    public CarCameraFollow cameraFollow;

    [Tooltip("The CarController to fade out after the cutscene.")]
    public CarController carController;

    [Tooltip("Seconds to wait after the window cross-fade before WinGame() is called.")]
    public float preWinGameDelay = 0f;

    [Tooltip("Seconds over which the car fades out (matches CarController.carFadeDuration by default).")]
    public float carFadeDuration = 1f;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TriggerCutscene()
    {
        if (isGameWon) return;
        StartCoroutine(PlayCutscene());
    }

    private IEnumerator PlayCutscene()
    {
        // Wait before starting the first animation
        if (cutsceneStartDelay > 0f)
            yield return new WaitForSeconds(cutsceneStartDelay);

        if (animations == null || animations.Length == 0)
        {
            ApplyFinalHouseState();
            WinGame();
            yield break;
        }

        int lastIndex = animations.Length - 1;

        for (int i = 0; i <= lastIndex; i++)
        {
            AnimationEntry entry = animations[i];
            if (entry.animObject == null) continue;

            bool isLast = (i == lastIndex);

            // ── Fade in ───────────────────────────────────────────────────
            SetEntryAlphas(entry, 0f);
            entry.animObject.SetActive(true);

            if (isLast && houseOpened != null)
            {
                // Enable house opened at alpha 0
                SetAllChildAlphas(houseOpened, 0f);
                houseOpened.SetActive(true);

                // Run all three fades in parallel: anim in, house opened in, house closed out
                Coroutine ci1 = entry.fadeInDuration > 0f ? StartCoroutine(FadeEntry(entry, 0f, 1f, entry.fadeInDuration))                                : null;
                Coroutine ci2 = entry.fadeInDuration > 0f ? StartCoroutine(FadeAllChildren(houseOpened, 0f, 1f, entry.fadeInDuration))                    : null;
                Coroutine ci3 = (houseClosed != null && entry.fadeInDuration > 0f) ? StartCoroutine(FadeAllChildren(houseClosed, 1f, 0f, entry.fadeInDuration)) : null;
                if (ci1 != null) yield return ci1;
                if (ci2 != null) yield return ci2;
                if (ci3 != null) yield return ci3;

                // Snap in case duration was 0
                if (entry.fadeInDuration <= 0f)
                {
                    SetEntryAlphas(entry, 1f);
                    SetAllChildAlphas(houseOpened, 1f);
                    if (houseClosed != null) SetAllChildAlphas(houseClosed, 0f);
                }
            }
            else
            {
                if (entry.fadeInDuration > 0f)
                    yield return StartCoroutine(FadeEntry(entry, 0f, 1f, entry.fadeInDuration));
                else
                    SetEntryAlphas(entry, 1f);
            }

            // ── Hold ──────────────────────────────────────────────────────
            if (entry.displayDuration > 0f)
                yield return new WaitForSeconds(entry.displayDuration);

            // ── Fade out ──────────────────────────────────────────────────
            if (isLast && houseOpened != null)
            {
                // Run all three fades in parallel: anim out, house opened out, house closed in
                Coroutine co1 = entry.fadeDuration > 0f ? StartCoroutine(FadeEntry(entry, 1f, 0f, entry.fadeDuration))                                    : null;
                Coroutine co2 = entry.fadeDuration > 0f ? StartCoroutine(FadeAllChildren(houseOpened, 1f, 0f, entry.fadeDuration))                        : null;
                Coroutine co3 = (houseClosed != null && entry.fadeDuration > 0f) ? StartCoroutine(FadeAllChildren(houseClosed, 0f, 1f, entry.fadeDuration)) : null;
                if (co1 != null) yield return co1;
                if (co2 != null) yield return co2;
                if (co3 != null) yield return co3;

                // Snap in case duration was 0
                if (entry.fadeDuration <= 0f)
                {
                    SetEntryAlphas(entry, 0f);
                    SetAllChildAlphas(houseOpened, 0f);
                    if (houseClosed != null) SetAllChildAlphas(houseClosed, 1f);
                }

                houseOpened.SetActive(false);
            }
            else
            {
                if (entry.fadeDuration > 0f)
                    yield return StartCoroutine(FadeEntry(entry, 1f, 0f, entry.fadeDuration));
            }

            entry.animObject.SetActive(false);

            if (entry.gapAfter > 0f)
                yield return new WaitForSeconds(entry.gapAfter);
        }

        yield return StartCoroutine(ApplyFinalHouseState());
    }

    private IEnumerator ApplyFinalHouseState()
    {
        // 1. Optional pause after the last animation ends
        if (houseFinishDelay > 0f)
            yield return new WaitForSeconds(houseFinishDelay);

        // 2. Camera focus + car fade simultaneously; wait for both to finish
        if (cameraFollow != null)
            cameraFollow.TriggerHouseFocus();

        Coroutine carFade = (carController != null)
            ? StartCoroutine(carController.FadeOutCar(carFadeDuration))
            : null;

        float focusDuration = (cameraFollow != null) ? cameraFollow.houseFocusMoveDuration : 0f;
        if (focusDuration > 0f)
            yield return new WaitForSeconds(focusDuration);

        if (carFade != null) yield return carFade;

        // 4. Now cross-fade the window: dark out, light in
        if (windowLight != null)
        {
            SetAllChildAlphas(windowLight, 0f);
            windowLight.SetActive(true);
        }

        Coroutine fadeOut = (windowDark  != null && houseFadeOutDuration > 0f)
            ? StartCoroutine(FadeAllChildren(windowDark,  1f, 0f, houseFadeOutDuration))
            : null;
        Coroutine fadeIn  = (windowLight != null && houseFadeInDuration  > 0f)
            ? StartCoroutine(FadeAllChildren(windowLight, 0f, 1f, houseFadeInDuration))
            : null;

        if (fadeOut != null) yield return fadeOut;
        if (fadeIn  != null) yield return fadeIn;

        if (windowDark != null)
            windowDark.SetActive(false);

        if (preWinGameDelay > 0f)
            yield return new WaitForSeconds(preWinGameDelay);

        WinGame();
    }

    /// <summary>Lerps the alpha of every child SpriteRenderer from <paramref name="from"/> to <paramref name="to"/>.</summary>
    private IEnumerator FadeAllChildren(GameObject obj, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAllChildAlphas(obj, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAllChildAlphas(obj, to);
    }

    /// <summary>Fades all children of entry.animObject AND entry.paperSprite together.</summary>
    private IEnumerator FadeEntry(AnimationEntry entry, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetEntryAlphas(entry, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetEntryAlphas(entry, to);
    }

    /// <summary>Sets alphas on all children of animObject, plus paperSprite even if it sits outside that hierarchy.</summary>
    private void SetEntryAlphas(AnimationEntry entry, float alpha)
    {
        SetAllChildAlphas(entry.animObject, alpha);

        // Explicitly drive paperSprite in case it lives outside animObject's hierarchy
        if (entry.paperSprite != null)
        {
            Color c = entry.paperSprite.color;
            c.a = alpha;
            entry.paperSprite.color = c;
        }
    }

    private void SetAllChildAlphas(GameObject obj, float alpha)
    {
        foreach (SpriteRenderer sr in obj.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
