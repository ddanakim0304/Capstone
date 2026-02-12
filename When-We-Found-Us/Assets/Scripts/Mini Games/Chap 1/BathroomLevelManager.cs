using UnityEngine;
using System.Collections;
using System;

public class BathroomLevelManager : MiniGameManager
{
    [Header("Mini Games")]
    public BrushTeethGameManager brushGame;
    public ShowerGameManager showerGame;

    [Header("Completion Settings")]
    [Tooltip("Time to wait after both are finished before changing scenes.")]
    public float delayAfterBothFinished = 1.0f;
    private bool sequenceStarted = false;

    void Update()
    {
        if (isGameWon || sequenceStarted) return;
        if (brushGame == null || showerGame == null)
        {
            Debug.LogError("BathroomLevelManager: Missing references to mini games!");
            return;
        }

        // Check if BOTH are finished
        if (brushGame.IsTaskFinished && showerGame.IsTaskFinished)
        {
            // Lock the sequence so Update doesn't call this again
            sequenceStarted = true;
            StartCoroutine(FinishLevelSequence());
        }
    }

    private IEnumerator FinishLevelSequence()
    {

        Debug.Log("Both mini games completed! Finishing level...");
        yield return new WaitForSeconds(delayAfterBothFinished);
        WinGame();
    }
}