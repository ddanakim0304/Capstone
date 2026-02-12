using UnityEngine;
using System.Collections;

public class ChoiceComicGameManager : GeneralComicManager
{
    [Header("Visual Settings")]
    [Tooltip("Scale of the option when selected (e.g. 1.2 is 20% bigger)")]
    public float optionScaleIntensity = 1.2f; 
    public Color normalColor = Color.white;
    public Color highlightColor = Color.cyan;
    public Color selectedColor = new Color(0.2f, 0.5f, 1f); // Blue-ish

    [Header("Timing")]
    [Tooltip("Time to wait after confirming selection before the result bubble appears.")]
    public float delayAfterChoice = 0.5f;

    // Internal state
    private ControllerInput activeController;
    private long lastEncoderCount;

    // Override the base logic to handle choice panels
    protected override IEnumerator ProcessExtraPanelLogic(ComicPanel panel)
    {
        if (panel.isChoicePanel && panel.choiceElements != null && panel.choiceElements.Count > 0)
        {
            // First, let base class handle choice animations
            yield return StartCoroutine(base.ProcessExtraPanelLogic(panel));
            
            // Then handle choice interaction
            yield return StartCoroutine(HandleChoiceLoop(panel));
        }
    }

    private IEnumerator HandleChoiceLoop(ComicPanel panel)
    {
        // 1. Setup Controller for THIS specific panel's player
        if (HardwareManager.Instance != null)
        {
            activeController = HardwareManager.Instance.GetController(panel.playerIndex);
            if (activeController != null) 
            {
                // Sync the encoder count so we don't get a jump immediately
                lastEncoderCount = activeController.EncoderCount;
            }
        }

        // 2. Choices are already animated by base class, just make them interactive

        int currentIndex = 0;
        bool confirmed = false;

        // 3. Input Loop
        while (!confirmed)
        {
            int inputDelta = 0;

            // --- A. Hardware Input (1 Tick = 1 Move) ---
            if (activeController != null && activeController.IsHardwareConnected)
            {
                long currentCount = activeController.EncoderCount;
                long rawDiff = currentCount - lastEncoderCount;
                
                // Update tracker immediately
                lastEncoderCount = currentCount;

                // Cast to int to update index
                inputDelta = (int)rawDiff; 
            }
            // --- B. Keyboard Fallback (Discrete Key Presses) ---
            else
            {
                // We use GetKeyDown for "Tick" like behavior, rather than GetAxis
                if (panel.playerIndex == 0) // P1
                {
                    if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) inputDelta = 1;
                    if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) inputDelta = -1;
                }
                else // P2
                {
                    if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.L)) inputDelta = 1;
                    if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.J)) inputDelta = -1;
                }
            }

            // --- C. Update Index ---
            if (inputDelta != 0)
            {
                currentIndex += inputDelta;
                
                // Clamp to ensure we stay within the list size
                currentIndex = Mathf.Clamp(currentIndex, 0, panel.choiceElements.Count - 1);
            }

            // --- D. Update Visuals ---
            for (int i = 0; i < panel.choiceElements.Count; i++)
            {
                if (panel.choiceElements[i] == null || panel.choiceElements[i].targetObj == null) continue;

                SpriteRenderer sr = panel.choiceElements[i].targetObj.GetComponent<SpriteRenderer>();
                Transform tf = panel.choiceElements[i].targetObj.transform;

                if (i == currentIndex)
                {
                    // Highlighted
                    if (sr) sr.color = highlightColor;
                    tf.localScale = Vector3.Lerp(tf.localScale, Vector3.one * optionScaleIntensity, Time.deltaTime * 15f);
                }
                else
                {
                    // Normal
                    if (sr) sr.color = normalColor;
                    tf.localScale = Vector3.Lerp(tf.localScale, Vector3.one, Time.deltaTime * 15f);
                }
            }

            // --- E. Check Confirm Button ---
            bool pressed = (activeController != null && activeController.IsButtonPressed);
            
            // Fallback keys based on Panel Player Index
            if (!pressed)
            {
                if (panel.playerIndex == 0 && Input.GetKeyDown(KeyCode.E)) pressed = true;
                if (panel.playerIndex == 1 && Input.GetKeyDown(KeyCode.Return)) pressed = true;
            }

            if (pressed) confirmed = true;

            yield return null;
        }

        // 4. Post Selection
        
        // Store the choice in the persistent GameManager
        if (panel.choiceElements[currentIndex] != null && panel.choiceElements[currentIndex].targetObj != null)
        {
            string choiceName = panel.choiceElements[currentIndex].targetObj.name;
            if (MainGameFlowManager.Instance != null)
            {
                MainGameFlowManager.Instance.RegisterChoice(choiceName);
            }
        }

        // Color the selected one
        if (panel.choiceElements[currentIndex] != null && panel.choiceElements[currentIndex].targetObj != null)
        {
            var sr = panel.choiceElements[currentIndex].targetObj.GetComponent<SpriteRenderer>();
            if (sr) sr.color = selectedColor;
        }

        // Hide others
        for (int i = 0; i < panel.choiceElements.Count; i++)
        {
            if (i != currentIndex && panel.choiceElements[i] != null && panel.choiceElements[i].targetObj != null) 
                panel.choiceElements[i].targetObj.SetActive(false);
        }

        // 5. Delay before Result
        if (delayAfterChoice > 0)
        {
            yield return new WaitForSeconds(delayAfterChoice);
        }

        // 6. Result Animation
        yield return StartCoroutine(PlayElementAnimation(panel.resultElement));

        yield return new WaitForSeconds(2.0f); 
        UnityEngine.SceneManagement.SceneManager.LoadScene("IntermediateScene");
    }
}