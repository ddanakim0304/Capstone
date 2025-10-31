using UnityEngine;
using TMPro;

public class WavelengthGameManager : MiniGameManager
{
    [Header("Player References")]
    public PlayerWaveController player1Wave;
    public PlayerWaveController player2Wave;

    [Header("Player Visuals")]
    [Tooltip("The GameObject holding the 'Normal' sprites for Player 1.")]
    public GameObject player1NormalVisuals;
    [Tooltip("The GameObject holding the 'Happy' sprites for Player 1.")]
    public GameObject player1HappyVisuals;
    [Tooltip("The GameObject holding the 'Normal' sprites for Player 2.")]
    public GameObject player2NormalVisuals;
    [Tooltip("The GameObject holding the 'Happy' sprites for Player 2.")]
    public GameObject player2HappyVisuals;
    
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

        if (matchText != null)
        {
            matchText.gameObject.SetActive(false);
        }

        // Set the initial visual state for both players to "Normal".
        if (player1NormalVisuals != null) player1NormalVisuals.SetActive(true);
        if (player1HappyVisuals != null) player1HappyVisuals.SetActive(false);
        if (player2NormalVisuals != null) player2NormalVisuals.SetActive(true);
        if (player2HappyVisuals != null) player2HappyVisuals.SetActive(false);
    }

    void Update()
    {
        if (isGameWon) return;
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
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
                matchText.gameObject.SetActive(true);
                float remainingTime = timeToWin - matchTimer;
                matchText.text = string.Format("Match!\n{0:F1}s", remainingTime);
            }

            if (matchTimer >= timeToWin)
            {
                // Switch the visuals from "Normal" to "Happy".
                if (player1NormalVisuals != null) player1NormalVisuals.SetActive(false);
                if (player1HappyVisuals != null) player1HappyVisuals.SetActive(true);
                if (player2NormalVisuals != null) player2NormalVisuals.SetActive(false);
                if (player2HappyVisuals != null) player2HappyVisuals.SetActive(true);
                
                // Hide the countdown text immediately upon winning.
                if (matchText != null)
                {
                    matchText.gameObject.SetActive(false);
                }

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