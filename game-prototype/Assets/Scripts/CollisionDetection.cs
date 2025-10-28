using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    [Tooltip("The tag of the object we are looking for (e.g., 'Player').")]
    public string targetTag = "Player";
    
    public event System.Action OnPlayerBump;

    private bool hasBumped = false;

    // This is the most important function. If you don't see its first message, the problem is your physics setup.
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(gameObject.name + " COLLIDED with " + collision.gameObject.name);

        if (hasBumped) 
        {
            Debug.Log(gameObject.name + ": Collision detected, but hasBumped is already true. Ignoring.");
            return;
        }

        // Check if the other object has the correct tag.
        if (collision.gameObject.CompareTag(targetTag))
        {
            Debug.Log(gameObject.name + ": The object it hit has the correct tag: '" + targetTag + "'.");
            hasBumped = true;
            
            // Check if anything is listening to our event.
            if (OnPlayerBump != null)
            {
                Debug.Log(gameObject.name + ": Firing the OnPlayerBump event NOW!");
                OnPlayerBump.Invoke();
            }
            else
            {
                Debug.LogError(gameObject.name + ": OnPlayerBump event was triggered, but NOBODY WAS LISTENING! Check your BumpingGameManager script.");
            }
        }
        else
        {
            Debug.LogWarning(gameObject.name + ": It collided with " + collision.gameObject.name + ", but its tag is '" + collision.gameObject.tag + "', not '" + targetTag + "'.");
        }
    }

    public void ResetBump()
    {
        hasBumped = false;
    }
}