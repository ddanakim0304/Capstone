using UnityEngine;

public class SlimeGameManager : MiniGameManager
{
    // Singleton pattern for easy access within the scene
    public static SlimeGameManager Instance { get; private set; }

    [Header("Game Settings")]
    [Tooltip("Do both slimes need to touch the cheese?")]
    public bool requireBothPlayers = false;

    private int playersOnGoal = 0;

    void Awake()
    {
        Instance = this;
    }

    public void PlayerReachedGoal()
    {
        playersOnGoal++;

        // Check win condition
        if (requireBothPlayers)
        {
            if (playersOnGoal >= 2)
            {
                Debug.Log("Both Slimes reached the cheese!");
                WinGame(); // Calls MainGameFlowManager
            }
        }
        else
        {
            Debug.Log("A Slime got the cheese!");
            WinGame(); // Calls MainGameFlowManager
        }
    }

    public void PlayerLeftGoal()
    {
        playersOnGoal--;
        if (playersOnGoal < 0) playersOnGoal = 0;
    }
}