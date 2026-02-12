using UnityEngine;
using System.Collections;

public class BumpingGameManager : MiniGameManager
{
    [Header("Player References")]
    public CollisionDetection player1Collision;
    public CollisionDetection player2Collision;
    
    [Header("Movement References")]
    public PlayerMover player1Mover;
    public PlayerMover player2Mover;

    [Header("Reaction References")]
    public BumpReaction player1Reaction;
    public BumpReaction player2Reaction;
    public float delayAfterBump = 1.0f;

    void Start()
    {
        // When a bump is detected, call HandleBumpDetected
        if (player1Collision != null) player1Collision.OnPlayerBump += HandleBumpDetected;
        if (player2Collision != null) player2Collision.OnPlayerBump += HandleBumpDetected;
    }

    private void HandleBumpDetected()
    {
        if (isGameWon) return;

        // Unsubscribe from further bump events to prevent multiple triggers.
        if (player1Collision != null) player1Collision.OnPlayerBump -= HandleBumpDetected;
        if (player2Collision != null) player2Collision.OnPlayerBump -= HandleBumpDetected;
        
        // Disable movement on both players
        if (player1Mover != null) player1Mover.canMove = false;
        if (player2Mover != null) player2Mover.canMove = false;
        
        // Trigger bump animation
        Vector3 diff = player2Reaction.transform.position - player1Reaction.transform.position;
        diff.y = 0;
        diff.z = 0;
        Vector3 direction = diff.normalized;

        // Fallback if positions are identical
        if (direction == Vector3.zero) direction = Vector3.right;

        player1Reaction.TriggerReaction(-direction);
        player2Reaction.TriggerReaction(direction);

        StartCoroutine(DelayedWin());
    }

    private IEnumerator DelayedWin()
    {
        // Wait for a short delay before declaring the win
        yield return new WaitForSeconds(delayAfterBump); 
        WinGame();
    }
}