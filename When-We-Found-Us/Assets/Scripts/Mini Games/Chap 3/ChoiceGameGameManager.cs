using UnityEngine;

public class SlimeGameManager : MiniGameManager
{

    // Singleton instance for global access
    public static SlimeGameManager Instance { get; private set; }

    [Header("Game Settings")]
    // If true, both players must reach the goal to win
    public bool requireBothPlayers = false;

    private int playersOnGoal = 0;

    void Awake()
    {
        Instance = this;
    }

    // Called when a player enters the goal trigger
    public void PlayerReachedGoal()
    {
        playersOnGoal++;

        if (requireBothPlayers)
        {
            if (playersOnGoal >= 2)
            {
                Debug.Log("Both Slimes reached the cheese!");
                WinGame();
            }
        }
        else
        {
            Debug.Log("A Slime got the cheese!");
            WinGame();
        }
    }

    public void PlayerLeftGoal()
    {
        playersOnGoal--;
        if (playersOnGoal < 0) playersOnGoal = 0;
    }
}