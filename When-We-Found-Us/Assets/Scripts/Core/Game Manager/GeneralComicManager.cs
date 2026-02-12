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
        
        // Cache renderers and masks (including children)
        elem.cachedRenderer = elem.targetObj.GetComponent<SpriteRenderer>(); // Legacy
        elem.cachedRenderers = elem.targetObj.GetComponentsInChildren<SpriteRenderer>(true);
        elem.cachedMasks = elem.targetObj.GetComponentsInChildren<SpriteMask>(true);

        // Store original cutoffs
        if (elem.cachedMasks != null && elem.cachedMasks.Length > 0)
        {
            elem.originalMaskCutoffs = new float[elem.cachedMasks.Length];
            for (int i = 0; i < elem.cachedMasks.Length; i++)
            {
                elem.originalMaskCutoffs[i] = elem.cachedMasks[i].alphaCutoff;
            }
        }
        else
        {
            elem.originalMaskCutoffs = new float[0];
        }

        if (elem.animationType == ComicAnimType.FadeIn)
        {
            SetElementAlpha(elem, 0);
            elem.targetObj.SetActive(false);
        }
        else if (elem.animationType == ComicAnimType.Pulse)
        {
            elem.targetObj.transform.localScale = Vector3.zero;
            elem.targetObj.SetActive(false);
        }
        else if (elem.animationType == ComicAnimType.SlideInFromLeft || elem.animationType == ComicAnimType.SlideInFromRight)
        {
            SetElementAlpha(elem, 0);
            elem.targetObj.SetActive(false);
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
            case ComicAnimType.FadeIn: anim = FadeIn(elem); break;
            case ComicAnimType.Pulse:  anim = PulseIn(elem); break;
            case ComicAnimType.Shake:  anim = ShakeObject(elem); break;
            case ComicAnimType.SlideInFromLeft: anim = SlideInFromLeft(elem); break;
            case ComicAnimType.SlideInFromRight: anim = SlideInFromRight(elem); break;
        }

        if (anim != null)
        {
            if (elem.waitForCompletion) yield return StartCoroutine(anim);
            else StartCoroutine(anim);
        }
    }

    protected IEnumerator FadeIn(ComicElement elem)
    {
        elem.targetObj.SetActive(true);
        float d = elem.duration;
        float t = 0f;
        while (t < d) 
        { 
            SetElementAlpha(elem, t/d); 
            t += Time.deltaTime; 
            yield return null; 
        }
        SetElementAlpha(elem, 1f);
    }

    protected IEnumerator PulseIn(ComicElement elem)
    {
        Transform t = elem.targetObj.transform;
        float d = elem.duration;
        float mag = elem.magnitude;
        Vector3 end = elem.originalScale;

        t.gameObject.SetActive(true);
        SetElementAlpha(elem, 0); // Start fully transparent
        
        Vector3 overshootScale = end * (1 + mag);
        Vector3 squashScale = end * (1 - mag * 0.5f);
        float stepDuration = d / 3.0f;
        
        // Animate through the three steps with fade-in: overshoot, squash, and settle.
        yield return AnimateScaleAndFade(elem, Vector3.zero, overshootScale, 0f, 1f, stepDuration);
        yield return AnimateScale(t, overshootScale, squashScale, stepDuration);
        yield return AnimateScale(t, squashScale, end, stepDuration);
    }

    protected IEnumerator ShakeObject(ComicElement elem)
    {
        Transform t = elem.targetObj.transform;
        t.gameObject.SetActive(true);
        Vector3 center = elem.originalPos;
        float d = elem.duration;
        float mag = elem.magnitude;

        float timer = 0f;
        while (timer < d)
        {
            t.localPosition = center + new Vector3(Random.Range(-1f,1f), Random.Range(-1f,1f), 0) * mag;
            timer += Time.deltaTime;
            yield return null;
        }
        t.localPosition = center;
    }

    protected IEnumerator SlideInFromLeft(ComicElement elem)
    {
        Transform t = elem.targetObj.transform;
        Vector3 targetPos = elem.originalPos;
        float d = elem.duration;
        float mag = elem.magnitude;
        
        t.gameObject.SetActive(true);
        Vector3 startPos = targetPos + Vector3.left * mag; // Start offset to the left
        t.localPosition = startPos;
        SetElementAlpha(elem, 0); // Start fully transparent
        
        // Animate position and fade simultaneously
        yield return AnimatePositionAndFade(elem, startPos, targetPos, 0f, 1f, d);
    }

    protected IEnumerator SlideInFromRight(ComicElement elem)
    {
        Transform t = elem.targetObj.transform;
        Vector3 targetPos = elem.originalPos;
        float d = elem.duration;
        float mag = elem.magnitude;
        
        t.gameObject.SetActive(true);
        Vector3 startPos = targetPos + Vector3.right * mag; // Start offset to the right
        t.localPosition = startPos;
        SetElementAlpha(elem, 0); // Start fully transparent
        
        // Animate position and fade simultaneously
        yield return AnimatePositionAndFade(elem, startPos, targetPos, 0f, 1f, d);
    }

    protected IEnumerator AnimateScale(Transform t, Vector3 s, Vector3 e, float d)
    {
        float time = 0f;
        while (time < d) { t.localScale = Vector3.Lerp(s, e, time/d); time += Time.deltaTime; yield return null; }
        t.localScale = e;
    }

    protected IEnumerator AnimateScaleAndFade(ComicElement elem, Vector3 scaleStart, Vector3 scaleEnd, float alphaStart, float alphaEnd, float d)
    {
        Transform t = elem.targetObj.transform;
        float time = 0f;
        while (time < d) 
        {
            float progress = time / d;
            t.localScale = Vector3.Lerp(scaleStart, scaleEnd, progress);
            SetElementAlpha(elem, Mathf.Lerp(alphaStart, alphaEnd, progress));
            time += Time.deltaTime;
            yield return null;
        }
        t.localScale = scaleEnd;
        SetElementAlpha(elem, alphaEnd);
    }

    protected IEnumerator AnimatePositionAndFade(ComicElement elem, Vector3 posStart, Vector3 posEnd, float alphaStart, float alphaEnd, float d)
    {
        Transform t = elem.targetObj.transform;
        float time = 0f;
        while (time < d)
        {
            float progress = time / d;
            t.localPosition = Vector3.Lerp(posStart, posEnd, progress);
            SetElementAlpha(elem, Mathf.Lerp(alphaStart, alphaEnd, progress));
            time += Time.deltaTime;
            yield return null;
        }
        t.localPosition = posEnd;
        SetElementAlpha(elem, alphaEnd);
    }

    // Helper to set alpha on all cached renderers and masks
    protected void SetElementAlpha(ComicElement elem, float a) 
    { 
        if (elem.cachedRenderers != null)
        {
            foreach (var r in elem.cachedRenderers)
            {
                SetAlpha(r, a);
            }
        }
        
        if (elem.cachedMasks != null)
        {
            // For masks, we invert the logic if we want to "fade in" (reveal).
            // Alpha 0 = Invisible (Hidden) -> Cutoff 1
            // Alpha 1 = Visible (Revealed) -> Original Cutoff (e.g. 0 or 0.2)
            
            // Lerp from 1 down to originalCutoff based on alpha 'a' (which goes 0->1)
            // if a=0, cutoff=1. if a=1, cutoff=original.
            
            for (int i = 0; i < elem.cachedMasks.Length; i++)
            {
                var m = elem.cachedMasks[i];
                if (m)
                {
                    float original = (elem.originalMaskCutoffs != null && i < elem.originalMaskCutoffs.Length) 
                                     ? elem.originalMaskCutoffs[i] : 0f;
                    
                    float cutoff = Mathf.Lerp(1f, original, a);
                    m.alphaCutoff = cutoff;
                }
            }
        }
    }

    protected void SetAlpha(SpriteRenderer r, float a) { if(r) { Color c = r.color; c.a = a; r.color = c; } }
}