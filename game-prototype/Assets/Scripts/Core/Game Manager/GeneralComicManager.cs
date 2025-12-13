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
            
            // Init choice elements (Hide them by default in the base class)
            if (panel.choiceOptions != null)
            {
                foreach(var opt in panel.choiceOptions) if(opt) opt.SetActive(false);
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
        // Base class does nothing here
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
            case ComicAnimType.Pulse:  anim = PulseIn(elem.targetObj.transform, elem.originalScale, elem.duration, elem.magnitude); break;
            case ComicAnimType.Shake:  anim = ShakeObject(elem.targetObj.transform, elem.originalPos, elem.duration, elem.magnitude); break;
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

    protected IEnumerator PulseIn(Transform t, Vector3 end, float d, float mag)
    {
        t.gameObject.SetActive(true);
        Vector3 over = end * (1 + mag);
        Vector3 squash = end * (1 - mag * 0.5f);
        float step = d / 3f;
        yield return AnimateScale(t, Vector3.zero, over, step);
        yield return AnimateScale(t, over, squash, step);
        yield return AnimateScale(t, squash, end, step);
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

    protected IEnumerator AnimateScale(Transform t, Vector3 s, Vector3 e, float d)
    {
        float time = 0f;
        while (time < d) { t.localScale = Vector3.Lerp(s, e, time/d); time += Time.deltaTime; yield return null; }
        t.localScale = e;
    }

    protected void SetAlpha(SpriteRenderer r, float a) { if(r) { Color c = r.color; c.a = a; r.color = c; } }
}