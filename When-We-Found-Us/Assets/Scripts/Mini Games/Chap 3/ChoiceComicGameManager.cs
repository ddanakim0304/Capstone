using UnityEngine;
using System.Collections;

public class ChoiceComicGameManager : GeneralComicManager
{
    [Header("Visual Settings")]
    public float optionScaleIntensity = 1.2f; 
    public Color normalColor = Color.white;
    public Color highlightColor = Color.cyan;
    public Color selectedColor = new Color(0.2f, 0.5f, 1f);

    [Header("Timing")]
    public float delayAfterChoice = 0.5f;


    private ControllerInput activeController;
    private long lastEncoderCount;


    protected override IEnumerator ProcessExtraPanelLogic(ComicPanel panel)
    {
        if (panel.isChoicePanel && panel.choiceElements != null && panel.choiceElements.Count > 0)
        {

            yield return StartCoroutine(base.ProcessExtraPanelLogic(panel));
            

            yield return StartCoroutine(HandleChoiceLoop(panel));
        }
    }

    // Manages the selection process for comic panels
    private IEnumerator HandleChoiceLoop(ComicPanel panel)
    {
        if (HardwareManager.Instance != null)
        {
            activeController = HardwareManager.Instance.GetController(panel.playerIndex);
            if (activeController != null) 
            {

                lastEncoderCount = activeController.EncoderCount;
            }
        }



        int currentIndex = 0;
        bool confirmed = false;


        while (!confirmed)
        {
            int inputDelta = 0;

            // Calculate input change from encoders
            if (activeController != null && activeController.IsHardwareConnected)
            {
                long currentCount = activeController.EncoderCount;
                long rawDiff = lastEncoderCount - currentCount;
                

                lastEncoderCount = currentCount;


                inputDelta += (int)rawDiff; 
            }


            // Keyboard fallback inputs
            if (panel.playerIndex == 0)
            {
                if (Input.GetKeyDown(KeyCode.D)) inputDelta += 1;
                if (Input.GetKeyDown(KeyCode.A)) inputDelta -= 1;
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) inputDelta += 1;
                if (Input.GetKeyDown(KeyCode.LeftArrow))  inputDelta -= 1;
            }


            if (inputDelta != 0)
            {
                int prevIndex = currentIndex;
                currentIndex += inputDelta;
                

                currentIndex = Mathf.Clamp(currentIndex, 0, panel.choiceElements.Count - 1);


                if (currentIndex != prevIndex)
                    PlayChoiceChangeSound(panel);
            }

            // Update scaling and colors for all choice options
            for (int i = 0; i < panel.choiceElements.Count; i++)
            {
                if (panel.choiceElements[i] == null || panel.choiceElements[i].targetObj == null) continue;


                SpriteRenderer sr = panel.choiceElements[i].targetObj.GetComponent<SpriteRenderer>();
                Transform tf = panel.choiceElements[i].targetObj.transform;

                if (i == currentIndex)
                {

                    if (sr) sr.color = highlightColor;
                    tf.localScale = Vector3.Lerp(tf.localScale, Vector3.one * optionScaleIntensity, Time.deltaTime * 15f);
                }
                else
                {

                    if (sr) sr.color = normalColor;
                    tf.localScale = Vector3.Lerp(tf.localScale, Vector3.one, Time.deltaTime * 15f);
                }
            }


            bool pressed = (activeController != null && activeController.IsButtonPressed);
            

            if (panel.playerIndex == 0 && Input.GetKeyDown(KeyCode.W)) pressed = true;
            if (panel.playerIndex == 1 && Input.GetKeyDown(KeyCode.UpArrow)) pressed = true;

            if (pressed) confirmed = true;

            yield return null;
        }

        // Finalize selection and register choice with main manager
        if (panel.choiceElements[currentIndex] != null && panel.choiceElements[currentIndex].targetObj != null)
        {
            string choiceName = panel.choiceElements[currentIndex].targetObj.name;
            if (MainGameFlowManager.Instance != null)
            {
                MainGameFlowManager.Instance.RegisterChoice(choiceName);
            }
        }


        if (panel.choiceElements[currentIndex] != null && panel.choiceElements[currentIndex].targetObj != null)
        {
            var sr = panel.choiceElements[currentIndex].targetObj.GetComponent<SpriteRenderer>();
            if (sr) sr.color = selectedColor;
        }


        for (int i = 0; i < panel.choiceElements.Count; i++)
        {
            if (i != currentIndex && panel.choiceElements[i] != null && panel.choiceElements[i].targetObj != null) 
                panel.choiceElements[i].targetObj.SetActive(false);
        }


        if (delayAfterChoice > 0)
        {
            yield return new WaitForSeconds(delayAfterChoice);
        }


        yield return StartCoroutine(PlayElementAnimation(panel.resultElement));

        yield return new WaitForSeconds(2.0f); 
        UnityEngine.SceneManagement.SceneManager.LoadScene("IntermediateScene");
    }
}