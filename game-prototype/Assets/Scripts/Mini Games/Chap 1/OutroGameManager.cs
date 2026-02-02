using UnityEngine;
using System.Collections;

public class Chap1IntroAnimationManager : MiniGameManager
{
    [Header("Characters")]
    public GameObject[] characters = new GameObject[2];
    public float appearanceDuration = 2f;

    [Header("Animations")]
    public GameObject[] animationSlots = new GameObject[2];
    public int cycleCount = 5;
    public float totalCycleDuration = 0.8f;

    void Start()
    {
        SetupInitialState();
        StartCoroutine(PlayAnimationSequence());
    }

    private void SetupInitialState()
    {
        // Hide characters initially
        foreach (var character in characters)
        {
            if (character != null) SetObjectAlpha(character, 0f);
        }

        // Hide animations initially
        foreach (var slot in animationSlots)
        {
            if (slot != null) slot.SetActive(false);
        }
    }

    private IEnumerator PlayAnimationSequence()
    {
        // Characters appear
        yield return StartCoroutine(CharacterAppearAnimation());

        // Animations cycle
        yield return StartCoroutine(AnimationCycling());

        // Move to next scene
        WinGame();
    }

    private IEnumerator CharacterAppearAnimation()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < appearanceDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.SmoothStep(0f, 1f, elapsedTime / appearanceDuration);
            
            foreach (var character in characters)
            {
                if (character != null) SetObjectAlpha(character, alpha);
            }
            
            yield return null;
        }

        foreach (var character in characters)
        {
            if (character != null) SetObjectAlpha(character, 1f);
        }
    }

    private IEnumerator AnimationCycling()
    {
        float timePerCycle = totalCycleDuration / cycleCount;
        float timePerSlot = timePerCycle / animationSlots.Length;
        
        for (int cycle = 0; cycle < cycleCount; cycle++)
        {
            // Cycle through each animation slot
            for (int i = 0; i < animationSlots.Length; i++)
            {
                // Hide all animations first
                foreach (var slot in animationSlots)
                {
                    if (slot != null) slot.SetActive(false);
                }
                
                // Show current animation
                if (animationSlots[i] != null)
                {
                    animationSlots[i].SetActive(true);
                }
                
                yield return new WaitForSeconds(timePerSlot);
            }
        }
        
        // Hide all animations at the end
        foreach (var slot in animationSlots)
        {
            if (slot != null) slot.SetActive(false);
        }
    }

    private void SetObjectAlpha(GameObject obj, float alpha)
    {
        if (obj == null) return;

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color color = sr.color;
            color.a = alpha;
            sr.color = color;
            return;
        }

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = alpha;
        }
    }
}