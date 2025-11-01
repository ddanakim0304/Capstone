using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SpeechBubbleController : MonoBehaviour
{
    public enum Choice { A, B }

    [Header("UI References")]
    public TextMeshProUGUI choiceA_Text;
    public TextMeshProUGUI choiceB_Text;
    // The Image selector reference is now removed.

    [Header("Selection Colors")]
    public Color selectedColor = new Color(0.2f, 0.4f, 1f); // A nice blue
    public Color normalColor = Color.black;

    private Choice currentChoice = Choice.A;
    private string originalTextA; // To store the text without the arrow
    private string originalTextB; // To store the text without the arrow

    public void Show(string textA, string textB)
    {
        gameObject.SetActive(true);
        choiceA_Text.gameObject.SetActive(true);
        choiceB_Text.gameObject.SetActive(true);

        // Store the original, clean text.
        originalTextA = textA;
        originalTextB = textB;
        
        currentChoice = Choice.A;
        UpdateSelectionVisuals();
    }

    public void SwitchSelection()
    {
        currentChoice = (currentChoice == Choice.A) ? Choice.B : Choice.A;
        UpdateSelectionVisuals();
    }

    public Choice ConfirmSelection()
    {
        if (currentChoice == Choice.A)
        {
            choiceB_Text.gameObject.SetActive(false);
            // On confirm, remove the arrow for a clean final look.
            choiceA_Text.text = originalTextA;
        }
        else
        {
            choiceA_Text.gameObject.SetActive(false);
            // On confirm, remove the arrow for a clean final look.
            choiceB_Text.text = originalTextB;
        }
        return currentChoice;
    }

    // This function now handles both text content and color changes.
    private void UpdateSelectionVisuals()
    {
        if (currentChoice == Choice.A)
        {
            // Set text content with the "->" selector
            choiceA_Text.text = "-> " + originalTextA;
            choiceB_Text.text = originalTextB; // No arrow

            // Set colors
            choiceA_Text.color = selectedColor;
            choiceB_Text.color = normalColor;
        }
        else // currentChoice is B
        {
            // Set text content with the "->" selector
            choiceA_Text.text = originalTextA; // No arrow
            choiceB_Text.text = "-> " + originalTextB;

            // Set colors
            choiceA_Text.color = normalColor;
            choiceB_Text.color = selectedColor;
        }
    }
}