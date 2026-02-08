using UnityEngine;

public abstract class MiniGameManager : MonoBehaviour
{
    protected bool isGameWon = false;

    // This is the method that individual mini-games will call when their win condition is met.
    protected virtual void WinGame()
    {
        // Prevent winning multiple times
        if (isGameWon) return; 
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