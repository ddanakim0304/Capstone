using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    [Tooltip("The tag of the object we are looking for (e.g., 'Player').")]
    public string targetTag = "Player";
    public event System.Action OnPlayerBump;
    private bool hasBumped = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        // If a bump has already been registered, ignore subsequent collision events.
        if (hasBumped) 
        {
            return;
        }

        if (collision.gameObject.CompareTag(targetTag))
        {   
            // Set the flag to true to lock this collision event.
            hasBumped = true;
            
            if (OnPlayerBump != null)
            {
                OnPlayerBump.Invoke();
            }
        }
    }

    // A public method to allow external scripts to reset the bump state.
    public void ResetBump()
    {
        hasBumped = false;
    }
}