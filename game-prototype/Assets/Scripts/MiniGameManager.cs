using UnityEngine;

public abstract class MiniGameManager : MonoBehaviour
{
    protected bool isGameWon = false;

    // This is the method that individual mini-games will call when their win condition is met.
    protected void WinGame()
    {
        if (isGameWon) return; // Prevent winning multiple times
        isGameWon = true;

        // Tell the main flow manager to proceed to the next scene.
        if (MainGameFlowManager.Instance != null)
        {
            MainGameFlowManager.Instance.MiniGameWon();
        }
        else
        {
            Debug.LogError("MainGameFlowManager not found! Cannot proceed to the next scene.");
        }
    }
}