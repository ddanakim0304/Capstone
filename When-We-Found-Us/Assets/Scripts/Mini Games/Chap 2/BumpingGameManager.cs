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

    [Header("Music")]
    public AudioClip Music;
    public AudioPan MusicPan = AudioPan.Center;
    [Range(0f, 4f)]
    public float musicVolume = 2.0f;
    public bool musicLoop = true;

    [Header("Heartbeat SFX")]
    public AudioClip player1Heartbeat;
    public AudioClip player2Heartbeat;
    [Range(0f, 1f)]
    public float heartbeatVolume = 1.0f;


    void Start()
    {
        // Start looping heartbeats for both players
        if (player1Heartbeat != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(player1Heartbeat, AudioPan.Left, heartbeatVolume, loop: true);
        if (player2Heartbeat != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(player2Heartbeat, AudioPan.Right, heartbeatVolume, loop: true);

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
        
        // Stop heartbeat SFX for both players
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFX(AudioPan.Left);
            AudioManager.Instance.StopSFX(AudioPan.Right);
        }

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
        if (Music != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(Music, MusicPan, musicVolume, musicLoop);
        }
        // Wait for a short delay before declaring the win
        yield return new WaitForSeconds(delayAfterBump); 
        WinGame();
    }
}