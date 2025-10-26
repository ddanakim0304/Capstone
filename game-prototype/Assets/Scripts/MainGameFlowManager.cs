using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MainGameFlowManager : MonoBehaviour
{
    public static MainGameFlowManager Instance { get; private set; }

    [Header("Game Sequence")]
    [Tooltip("Add the names of your mini-game scenes IN ORDER.")]
    public List<string> sceneNames;

    private int currentSceneIndex = 0;

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
        Debug.Log($"Mini-game '{sceneNames[currentSceneIndex]}' was won!");
        currentSceneIndex++;

        if (currentSceneIndex < sceneNames.Count)
        {
            Debug.Log($"Loading next mini-game: {sceneNames[currentSceneIndex]}");
            SceneManager.LoadScene(sceneNames[currentSceneIndex]);
        }
        else
        {
            Debug.Log("--- ENTIRE GAME COMPLETED! ---");
            // Here you can load a final "Thank you for playing" scene or trigger the finale.
        }
    }
}