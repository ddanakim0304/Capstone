using UnityEngine;

public class CheeseGoal : MonoBehaviour
{
    [Header("FX")]
    public ParticleSystem winParticles;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object is a player
        if (other.CompareTag("Slime"))
        {
            if (winParticles != null) winParticles.Play();
            
            // Notify the manager
            SlimeGameManager.Instance.PlayerReachedGoal();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Slime"))
        {
            SlimeGameManager.Instance.PlayerLeftGoal();
        }
    }
}