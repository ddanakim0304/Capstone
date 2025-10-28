using UnityEngine;

public class BumpingGame_Manager : MiniGameManager
{
    [Header("Player References")]
    public PlayerMover player1;
    public PlayerMover player2;

    [Header("Game Rules")]
    [Tooltip("How close the players need to be to trigger the 'bump'.")]
    public float bumpDistance = 1.0f;

    void Update()
    {
        if (isGameWon) return; // Stop checking after winning.
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        if (player1 == null || player2 == null) return;

        // Calculate the distance between the two players.
        float distance = Vector3.Distance(player1.transform.position, player2.transform.position);

        // If they are close enough, they "bump" and the game is won.
        if (distance < bumpDistance)
        {
            WinGame();
        }
    }
}