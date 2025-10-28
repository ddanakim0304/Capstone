using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

public class OpeningSceneUI : MonoBehaviour
{
    public void StartGame()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);

        Debug.Log("Start Button Clicked! Loading next scene...");
    }
}