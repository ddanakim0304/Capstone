// MainGameFlowManager.cs (REVISED AND IMPROVED)
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameFlowManager : MonoBehaviour
{
    public static MainGameFlowManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
    public void MiniGameWon()
    {
        // Get the index of the scene we are currently in.
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Calculate the index of the next scene.
        int nextSceneIndex = currentSceneIndex + 1;

        // Check if there IS a next scene in the build settings.
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"Mini-game {currentSceneIndex} won! Loading next scene: {nextSceneIndex}");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("--- FINAL SCENE COMPLETED! ENTIRE GAME WON! ---");
        }
    }
}