// WavelengthGame_Manager.cs (CORRECTED VERSION)
using UnityEngine;

public class WavelengthGameManager : MiniGameManager
{
    [Header("Player References")]
    public PlayerWaveController player1Wave;
    public PlayerWaveController player2Wave;
    
    [Header("Game Rules")]
    public float matchThreshold = 0.1f;
    public float timeToWin = 3f;

    [Header("Visual Feedback")]
    // --- THIS IS THE FIX ---
    // We are now looking for LineRenderers, not SpriteRenderers.
    public LineRenderer p1LineRenderer;
    public LineRenderer p2LineRenderer;
    public Color matchedColor = Color.white;
    
    private float matchTimer = 0f;
    private Color p1InitialColor;
    private Color p2InitialColor;

    void Start()
    {
        // Store the initial color from the LineRenderer's gradient.
        if(p1LineRenderer) p1InitialColor = p1LineRenderer.startColor;
        if(p2LineRenderer) p2InitialColor = p2LineRenderer.startColor;
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
            
            // --- THIS CODE NOW WORKS ---
            // It correctly targets the material of the LineRenderer.
            if(p1LineRenderer) p1LineRenderer.startColor = p1LineRenderer.endColor = matchedColor;
            if(p2LineRenderer) p2LineRenderer.startColor = p2LineRenderer.endColor = matchedColor;

            if (matchTimer >= timeToWin)
            {
                WinGame();
            }
        }
        else
        {
            matchTimer = 0f;

            // Revert colors when not matching.
            if(p1LineRenderer) p1LineRenderer.startColor = p1LineRenderer.endColor = p1InitialColor;
            if(p2LineRenderer) p2LineRenderer.startColor = p2LineRenderer.endColor = p2InitialColor;
        }
    }
}