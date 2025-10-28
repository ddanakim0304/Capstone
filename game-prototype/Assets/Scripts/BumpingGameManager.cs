using UnityEngine;
using System.Collections;

public class BumpingGameManager : MiniGameManager
{
    [Header("Player References")]
    public CollisionDetection player1Collision;
    public CollisionDetection player2Collision;
    
    // We need a reference to the PlayerMover scripts to disable them.
    [Header("Movement References")]
    public PlayerMover player1Mover;
    public PlayerMover player2Mover;

    [Header("Reaction References")]
    public BumpReaction player1Reaction;
    public BumpReaction player2Reaction;

    [Tooltip("How long to wait after the bump before loading the next scene.")]
    public float delayAfterBump = 1.0f;

    void Start()
    {
        if (player1Collision != null) player1Collision.OnPlayerBump += HandleBumpDetected;
        if (player2Collision != null) player2Collision.OnPlayerBump += HandleBumpDetected;
    }

    private void HandleBumpDetected()
    {
        if (isGameWon) return;

        // Unsubscribe immediately
        if (player1Collision != null) player1Collision.OnPlayerBump -= HandleBumpDetected;
        if (player2Collision != null) player2Collision.OnPlayerBump -= HandleBumpDetected;
        
        // Disable movement on both players immediately.
        if (player1Mover != null) player1Mover.canMove = false;
        if (player2Mover != null) player2Mover.canMove = false;
        
        // --- TRIGGER THE ANIMATION ---
        Vector3 direction = (player2Reaction.transform.position - player1Reaction.transform.position).normalized;
        player1Reaction.TriggerReaction(-direction);
        player2Reaction.TriggerReaction(direction);

        StartCoroutine(DelayedWin());
    }

    private IEnumerator DelayedWin()
    {
        yield return new WaitForSeconds(delayAfterBump); 
        WinGame();
    }
}