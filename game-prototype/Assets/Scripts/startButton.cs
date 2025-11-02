using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

public class OpeningSceneUI : MonoBehaviour
{
    public void StartGame()
    {
        // Load the next scene in the build index
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);
    }
}