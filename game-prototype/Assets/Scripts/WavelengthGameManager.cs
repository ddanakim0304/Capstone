using UnityEngine;
using TMPro;

public class WavelengthGameManager : MiniGameManager
{
    [Header("Player References")]
    public PlayerWaveController player1Wave;
    public PlayerWaveController player2Wave;
    
    [Header("UI References")]
    [Tooltip("The TextMeshPro object that displays the 'Match!' countdown.")]
    public TextMeshProUGUI matchText;
    [Header("Game Rules")]
    public float matchThreshold = 0.1f;
    public float timeToWin = 3f;

    [Header("Visual Feedback")]
    public LineRenderer p1LineRenderer;
    public LineRenderer p2LineRenderer;
    public Color matchedColor = Color.white;
    
    private float matchTimer = 0f;
    private Color p1InitialColor;
    private Color p2InitialColor;

    void Start()
    {
        if(p1LineRenderer) p1InitialColor = p1LineRenderer.startColor;
        if(p2LineRenderer) p2InitialColor = p2LineRenderer.startColor;

        // Ensure the match text is hidden at the start of the game.
        if (matchText != null)
        {
            matchText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isGameWon) return;
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        // Assuming your PlayerWaveController has a public 'Frequency' property.
        float frequency1 = player1Wave.Frequency;
        float frequency2 = player2Wave.Frequency;
        float frequencyDifference = Mathf.Abs(frequency1 - frequency2);

        if (frequencyDifference < matchThreshold)
        {
            matchTimer += Time.deltaTime;
            
            if(p1LineRenderer) p1LineRenderer.startColor = p1LineRenderer.endColor = matchedColor;
            if(p2LineRenderer) p2LineRenderer.startColor = p2LineRenderer.endColor = matchedColor; 
            if (matchText != null)
            {
                // Make the text visible.
                matchText.gameObject.SetActive(true);
                
                // Calculate remaining time and format the string. F1 = one decimal place.
                float remainingTime = timeToWin - matchTimer;
                matchText.text = string.Format("Match!\n{0:F1}s", remainingTime);
            }

            if (matchTimer >= timeToWin)
            {
                WinGame();
            }
        }
        else
        {
            matchTimer = 0f;

            if(p1LineRenderer) p1LineRenderer.startColor = p1LineRenderer.endColor = p1InitialColor;
            if(p2LineRenderer) p2LineRenderer.startColor = p2LineRenderer.endColor = p2InitialColor;
            if (matchText != null)
            {
                matchText.gameObject.SetActive(false);
            }
        }
    }
}