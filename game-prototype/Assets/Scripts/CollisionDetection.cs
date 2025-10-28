using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    [Tooltip("The tag of the object we are looking for (e.g., 'Player').")]
    public string targetTag = "Player";
    
    // Event broadcasted when a collision with the target is detected.
    public event System.Action OnPlayerBump;

    private bool hasBumped = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasBumped) return;

        // Check if the other object has the target tag (the other player)
        if (collision.gameObject.CompareTag(targetTag))
        {
            hasBumped = true;
            
            // Trigger the event for the manager to listen to
            if (OnPlayerBump != null)
            {
                OnPlayerBump.Invoke();
            }
        }
    }

    // Optional: Call this if you want to reuse the script later.
    public void ResetBump()
    {
        hasBumped = false;
    }
}