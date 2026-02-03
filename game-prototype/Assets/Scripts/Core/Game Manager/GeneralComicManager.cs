using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GeneralComicManager : MiniGameManager
{
    [Header("Comic Configuration")]
    public List<ComicPanel> comicPanels;

    protected virtual void Start()
    {
        InitializeScene();
        StartCoroutine(PlayComicSequence());
    }

    protected void InitializeScene()
    {
        foreach (var panel in comicPanels)
        {
            // Init standard elements
            foreach (var elem in panel.elements) InitElementState(elem);
            
            // Init choice elements with proper animation setup
            if (panel.choiceElements != null)
            {
                foreach(var choiceElem in panel.choiceElements) InitElementState(choiceElem);
            }
            InitElementState(panel.resultElement);
        }
    }

    protected void InitElementState(ComicElement elem)
    {
        if (elem == null || elem.targetObj == null) return;

        elem.originalScale = elem.targetObj.transform.localScale;
        elem.originalPos = elem.targetObj.transform.localPosition;
        elem.cachedRenderer = elem.targetObj.GetComponent<SpriteRenderer>();

        if (elem.animationType == ComicAnimType.FadeIn)
        {
            if (elem.cachedRenderer != null) SetAlpha(elem.cachedRenderer, 0);
            elem.targetObj.SetActive(true);
        }
        else if (elem.animationType == ComicAnimType.Pulse)
        {
            elem.targetObj.transform.localScale = Vector3.zero;
            elem.targetObj.SetActive(false);
        }
        else if (elem.animationType == ComicAnimType.SlideInFromLeft || elem.animationType == ComicAnimType.SlideInFromRight)
        {
            if (elem.cachedRenderer != null) SetAlpha(elem.cachedRenderer, 0);
            elem.targetObj.SetActive(true);
        }
        else if (elem.animationType != ComicAnimType.Shake) // Shake usually implies visible
        {
             elem.targetObj.SetActive(false);
        }
    }

    protected IEnumerator PlayComicSequence()
    {
        foreach (var panel in comicPanels)
        {
            // 1. Play Standard Animations
            foreach (var elem in panel.elements)
            {
                yield return StartCoroutine(PlayElementAnimation(elem));
            }

            // 2. Allow Child Class to insert Logic here (Choice)
            yield return StartCoroutine(ProcessExtraPanelLogic(panel));

            // 3. Wait
            if (panel.delayAfterPanel > 0)
                yield return new WaitForSeconds(panel.delayAfterPanel);
        }

        WinGame();
    }

    // Virtual method for the Child class to override
    protected virtual IEnumerator ProcessExtraPanelLogic(ComicPanel panel)
    {
        // Base class: Show choice elements if this is a choice panel
        if (panel.isChoicePanel && panel.choiceElements != null && panel.choiceElements.Count > 0)
        {
            // Wait before showing choices
            if (panel.delayBeforeChoices > 0)
                yield return new WaitForSeconds(panel.delayBeforeChoices);
                
            // Animate all choice elements
            foreach (var choiceElem in panel.choiceElements)
            {
                yield return StartCoroutine(PlayElementAnimation(choiceElem));
            }
        }
        yield break;
    }

    // --- Animation Logic (Protected so Child can use them) ---

    protected IEnumerator PlayElementAnimation(ComicElement elem)
    {
        if (elem.targetObj == null) yield break;
        if (elem.delayBefore > 0) yield return new WaitForSeconds(elem.delayBefore);

        IEnumerator anim = null;
        switch (elem.animationType)
        {
            case ComicAnimType.FadeIn: anim = FadeIn(elem.cachedRenderer, elem.duration); break;
            case ComicAnimType.Pulse:  anim = PulseIn(elem.targetObj.transform, elem.cachedRenderer, elem.originalScale, elem.duration, elem.magnitude); break;
            case ComicAnimType.Shake:  anim = ShakeObject(elem.targetObj.transform, elem.originalPos, elem.duration, elem.magnitude); break;
            case ComicAnimType.SlideInFromLeft: anim = SlideInFromLeft(elem.targetObj.transform, elem.cachedRenderer, elem.originalPos, elem.duration, elem.magnitude); break;
            case ComicAnimType.SlideInFromRight: anim = SlideInFromRight(elem.targetObj.transform, elem.cachedRenderer, elem.originalPos, elem.duration, elem.magnitude); break;
        }

        if (anim != null)
        {
            if (elem.waitForCompletion) yield return StartCoroutine(anim);
            else StartCoroutine(anim);
        }
    }

    protected IEnumerator FadeIn(SpriteRenderer r, float d)
    {
        if (!r) yield break;
        r.gameObject.SetActive(true);
        float t = 0f;
        while (t < d) { SetAlpha(r, t/d); t += Time.deltaTime; yield return null; }
        SetAlpha(r, 1f);
    }

    protected IEnumerator PulseIn(Transform t, SpriteRenderer r, Vector3 end, float d, float mag)
    {
        if (t == null) yield break;
        
        t.gameObject.SetActive(true);
        if (r) SetAlpha(r, 0); // Start fully transparent
        
        Vector3 overshootScale = end * (1 + mag);
        Vector3 squashScale = end * (1 - mag * 0.5f);
        float stepDuration = d / 3.0f;
        
        // Animate through the three steps with fade-in: overshoot, squash, and settle.
        yield return AnimateScaleAndFade(t, r, Vector3.zero, overshootScale, 0f, 1f, stepDuration);
        yield return AnimateScale(t, overshootScale, squashScale, stepDuration);
        yield return AnimateScale(t, squashScale, end, stepDuration);
    }

    protected IEnumerator ShakeObject(Transform t, Vector3 center, float d, float mag)
    {
        t.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < d)
        {
            t.localPosition = center + new Vector3(Random.Range(-1f,1f), Random.Range(-1f,1f), 0) * mag;
            timer += Time.deltaTime;
            yield return null;
        }
        t.localPosition = center;
    }

    protected IEnumerator SlideInFromLeft(Transform t, SpriteRenderer r, Vector3 targetPos, float d, float mag)
    {
        if (t == null) yield break;
        
        t.gameObject.SetActive(true);
        Vector3 startPos = targetPos + Vector3.left * mag; // Start offset to the left
        t.localPosition = startPos;
        if (r) SetAlpha(r, 0); // Start fully transparent
        
        // Animate position and fade simultaneously
        yield return AnimatePositionAndFade(t, r, startPos, targetPos, 0f, 1f, d);
    }

    protected IEnumerator SlideInFromRight(Transform t, SpriteRenderer r, Vector3 targetPos, float d, float mag)
    {
        if (t == null) yield break;
        
        t.gameObject.SetActive(true);
        Vector3 startPos = targetPos + Vector3.right * mag; // Start offset to the right
        t.localPosition = startPos;
        if (r) SetAlpha(r, 0); // Start fully transparent
        
        // Animate position and fade simultaneously
        yield return AnimatePositionAndFade(t, r, startPos, targetPos, 0f, 1f, d);
    }

    protected IEnumerator AnimateScale(Transform t, Vector3 s, Vector3 e, float d)
    {
        float time = 0f;
        while (time < d) { t.localScale = Vector3.Lerp(s, e, time/d); time += Time.deltaTime; yield return null; }
        t.localScale = e;
    }

    protected IEnumerator AnimateScaleAndFade(Transform t, SpriteRenderer r, Vector3 scaleStart, Vector3 scaleEnd, float alphaStart, float alphaEnd, float d)
    {
        float time = 0f;
        while (time < d) 
        {
            float progress = time / d;
            t.localScale = Vector3.Lerp(scaleStart, scaleEnd, progress);
            if (r) SetAlpha(r, Mathf.Lerp(alphaStart, alphaEnd, progress));
            time += Time.deltaTime;
            yield return null;
        }
        t.localScale = scaleEnd;
        if (r) SetAlpha(r, alphaEnd);
    }

    protected IEnumerator AnimatePositionAndFade(Transform t, SpriteRenderer r, Vector3 posStart, Vector3 posEnd, float alphaStart, float alphaEnd, float d)
    {
        float time = 0f;
        while (time < d)
        {
            float progress = time / d;
            t.localPosition = Vector3.Lerp(posStart, posEnd, progress);
            if (r) SetAlpha(r, Mathf.Lerp(alphaStart, alphaEnd, progress));
            time += Time.deltaTime;
            yield return null;
        }
        t.localPosition = posEnd;
        if (r) SetAlpha(r, alphaEnd);
    }

    protected void SetAlpha(SpriteRenderer r, float a) { if(r) { Color c = r.color; c.a = a; r.color = c; } }
}