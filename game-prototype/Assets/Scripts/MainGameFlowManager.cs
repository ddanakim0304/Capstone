// MainGameFlowManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameFlowManager : MonoBehaviour
{
    // A singleton instance for easy access from other scripts
    public static MainGameFlowManager Instance { get; private set; }

    void Awake()
    {
        //ensure only one instance of the manager exists.
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
        // Get the build index of the currently active scene.
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Figure out the index for the next scene in the build order.
        int nextSceneIndex = currentSceneIndex + 1;

        // Check if a next scene actually exists in the build settings.
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"Mini-game {currentSceneIndex} won! Loading next scene: {nextSceneIndex}");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            // If there are no more scenes, the player has finished the entire game.
            Debug.Log("FINAL SCENE COMPLETED! ENTIRE GAME WON!");
        }
    }
}