using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FightGameManager : MiniGameManager
{
    public enum FightPhase
    {
        P1_Argument,
        P2_Argument,
        P1_Rebalance,
        Free_Fight
    }

    [Header("UI References")]
    [Tooltip("Green Bar")]
    public RectTransform p1BarRect; 
    [Tooltip("Yellow Bar")]
    public RectTransform p2BarRect; 
    [Tooltip("Assign the 'FightContainer' here to shake the whole bar")]
    public Transform shakeContainer; 

    [Header("Story Bubbles")]
    public GameObject p1Bubble;
    public GameObject p2Bubble;
    public GameObject bothBubble; // The "FIGHT!" signal

    [Header("Gameplay Settings")]
    public float clickPower = 0.05f;
    public float smoothingSpeed = 10f;
    public float phaseTransitionDelay = 0.8f; // Pause between phases to build tension

    [Header("Phase Targets")]
    public float argumentCapMax = 0.9f; // Phase 1 Target
    public float argumentCapMin = 0.1f; // Phase 2 Target
    // Phase 3 Target is hardcoded to 0.5 (Middle)

    [Header("Visual Juice")]
    public float punchScaleAmount = 1.1f;
    public float punchDuration = 0.15f;
    public float fightStartShake = 50f; // Magnitude of shake when Free Fight starts

    // --- Internal State ---
    private FightPhase currentPhase = FightPhase.P1_Argument;
    private bool isTransitioning = false; 

    private ControllerInput p1_controller;
    private ControllerInput p2_controller;
    private bool p1_wasPressed = false;
    private bool p2_wasPressed = false;

    private float targetBalance = 0.5f; 
    private float currentBalance = 0.5f;

    private Vector3 p1OriginalScale;
    private Vector3 p2OriginalScale;
    private Vector3 containerOriginalPos;
    private Coroutine p1PunchRoutine;
    private Coroutine p2PunchRoutine;

    void Start()
    {
        // 1. Hardware Setup
        if (HardwareManager.Instance != null)
        {
            p1_controller = HardwareManager.Instance.GetController(0);
            p2_controller = HardwareManager.Instance.GetController(1);
        }

        // 2. Cache initial layout so we don't distort them
        if (p1BarRect != null) p1OriginalScale = p1BarRect.localScale;
        if (p2BarRect != null) p2OriginalScale = p2BarRect.localScale;
        if (shakeContainer != null) containerOriginalPos = shakeContainer.localPosition;

        // 3. Initialize Game State
        currentPhase = FightPhase.P1_Argument;
        targetBalance = 0.5f; 
        currentBalance = 0.5f;

        UpdateBubbles();
        UpdateVisuals(true); 
    }

    void Update()
    {
        if (isGameWon) return;

        HandleInput();
        CalculateBalance();
        UpdateVisuals(false); 
        CheckPhaseConditions();
    }

    private void HandleInput()
    {
        if (isTransitioning) return; 

        bool p1IsPressed = (p1_controller != null && p1_controller.IsButtonPressed);
        bool p2IsPressed = (p2_controller != null && p2_controller.IsButtonPressed);

        // --- P1 INPUT LOGIC ---
        // P1 active during: Phase 1 (Start), Phase 3 (Rebalance), Phase 4 (Fight)
        if (currentPhase == FightPhase.P1_Argument || 
            currentPhase == FightPhase.P1_Rebalance || 
            currentPhase == FightPhase.Free_Fight)
        {
            if (p1IsPressed && !p1_wasPressed)
            {
                targetBalance += clickPower;
                TriggerVisualPunch(p1BarRect, p1OriginalScale, ref p1PunchRoutine);
            }
        }

        // --- P2 INPUT LOGIC ---
        // P2 active during: Phase 2 (Retort), Phase 4 (Fight)
        if (currentPhase == FightPhase.P2_Argument || 
            currentPhase == FightPhase.Free_Fight)
        {
            if (p2IsPressed && !p2_wasPressed)
            {
                targetBalance -= clickPower;
                TriggerVisualPunch(p2BarRect, p2OriginalScale, ref p2PunchRoutine);
            }
        }

        p1_wasPressed = p1IsPressed;
        p2_wasPressed = p2IsPressed;
    }

    private void CalculateBalance()
    {
        // --- APPLY CAPS BASED ON PHASE ---
        if (currentPhase == FightPhase.P1_Argument)
        {
            // Cap at 0.9 (Leave a sliver of yellow)
            targetBalance = Mathf.Clamp(targetBalance, 0.0f, argumentCapMax);
        }
        else if (currentPhase == FightPhase.P2_Argument)
        {
            // Cap at 0.1 (Leave a sliver of green)
            targetBalance = Mathf.Clamp(targetBalance, argumentCapMin, 1.0f);
        }
        else if (currentPhase == FightPhase.P1_Rebalance)
        {
            // Cap at 0.5 (Force stop at middle)
            targetBalance = Mathf.Clamp(targetBalance, 0.0f, 0.5f);
        }
        else // Free Fight
        {
            // Full range allowed
            targetBalance = Mathf.Clamp01(targetBalance);
        }

        currentBalance = Mathf.Lerp(currentBalance, targetBalance, smoothingSpeed * Time.deltaTime);
    }

    private void CheckPhaseConditions()
    {
        if (isTransitioning) return;

        // 1. P1 Argument Done?
        if (currentPhase == FightPhase.P1_Argument)
        {
            if (currentBalance >= (argumentCapMax - 0.01f))
            {
                StartCoroutine(TransitionToPhase(FightPhase.P2_Argument));
            }
        }
        // 2. P2 Argument Done?
        else if (currentPhase == FightPhase.P2_Argument)
        {
            if (currentBalance <= (argumentCapMin + 0.01f))
            {
                // Next: P1 has to push it back to the middle
                StartCoroutine(TransitionToPhase(FightPhase.P1_Rebalance));
            }
        }
        // 3. P1 Rebalance Done? (Reached Middle)
        else if (currentPhase == FightPhase.P1_Rebalance)
        {
            if (currentBalance >= 0.49f)
            {
                // Next: Free Fight!
                StartCoroutine(TransitionToPhase(FightPhase.Free_Fight));
            }
        }
        // 4. Fight Win?
        else if (currentPhase == FightPhase.Free_Fight)
        {
            if (currentBalance >= 0.99f)
            {
                Debug.Log("P1 Wins!");
                currentBalance = 1.0f;
                UpdateVisuals(true);
                StartCoroutine(WinSequence());
            }
            else if (currentBalance <= 0.01f)
            {
                Debug.Log("P2 Wins!");
                currentBalance = 0.0f;
                UpdateVisuals(true);
                StartCoroutine(WinSequence());
            }
        }
    }

    private IEnumerator TransitionToPhase(FightPhase nextPhase)
    {
        isTransitioning = true;
        
        // Short pause to signify a "turn change"
        yield return new WaitForSeconds(phaseTransitionDelay);

        currentPhase = nextPhase;
        UpdateBubbles();

        // Special Effect: If starting the FREE FIGHT, shake the screen!
        if (nextPhase == FightPhase.Free_Fight)
        {
            StartCoroutine(ShakeContainer());
        }
        
        isTransitioning = false;
    }

    private void UpdateBubbles()
    {
        // Reset all
        if(p1Bubble) p1Bubble.SetActive(false);
        if(p2Bubble) p2Bubble.SetActive(false);
        if(bothBubble) bothBubble.SetActive(false);

        // Enable specific bubble for phase
        switch (currentPhase)
        {
            case FightPhase.P1_Argument:
                if(p1Bubble) p1Bubble.SetActive(true);
                break;
            case FightPhase.P2_Argument:
                if(p2Bubble) p2Bubble.SetActive(true);
                break;
            case FightPhase.P1_Rebalance:
                // Reuse P1 bubble (or keep P2 bubble off, or show P1 again)
                if(p1Bubble) p1Bubble.SetActive(true);
                break;
            case FightPhase.Free_Fight:
                if(bothBubble) bothBubble.SetActive(true); // "FIGHT!" graphic
                break;
        }
    }

    private void UpdateVisuals(bool instant)
    {
        if (p1BarRect == null || p2BarRect == null) return;
        float val = instant ? targetBalance : currentBalance;
        
        // P1 Anchors 0 -> Value
        p1BarRect.anchorMax = new Vector2(val, p1BarRect.anchorMax.y);
        // P2 Anchors Value -> 1
        p2BarRect.anchorMin = new Vector2(val, p2BarRect.anchorMin.y);
    }

    private IEnumerator ShakeContainer()
    {
        if (shakeContainer == null) yield break;
        
        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Decay shake over time for more natural feel
            float intensity = 1f - (elapsed / duration);
            float currentShake = fightStartShake * intensity;
            
            float x = Random.Range(-1f, 1f) * currentShake;
            float y = Random.Range(-1f, 1f) * currentShake;
            
            shakeContainer.localPosition = containerOriginalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeContainer.localPosition = containerOriginalPos;
    }

    private IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(1.0f);
        
        WinGame(); // Transitions to next scene

        Debug.Log("Next scene");
    }

    private void TriggerVisualPunch(RectTransform target, Vector3 baseScale, ref Coroutine routine)
    {
        if (target == null) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PunchScale(target, baseScale));
    }

    private IEnumerator PunchScale(RectTransform target, Vector3 baseScale)
    {
        Vector3 punchScale = baseScale * punchScaleAmount;
        float timer = 0f;
        while(timer < punchDuration)
        {
            target.localScale = Vector3.Lerp(punchScale, baseScale, timer / punchDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        target.localScale = baseScale;
    }
}