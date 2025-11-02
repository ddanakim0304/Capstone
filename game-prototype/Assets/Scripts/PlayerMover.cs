using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [Tooltip("0 for Player 1, 1 for Player 2")]
    public int playerIndex;
    public bool canMove = true; 

    [Header("Control Sensitivity")]
    public float keyboardSensitivity = 5f;

    void Update()
    {
        // If we can't move, just stop the function right here.
        if (!canMove)
        {
            return;
        }
        
        // Determine which input axis to use based on the player index.
        string axisName = (playerIndex == 0) ? "Horizontal_P1" : "Horizontal_P2";
        float movement = Input.GetAxis(axisName) * keyboardSensitivity * Time.deltaTime;

        // Move the character horizontally.
        transform.Translate(movement, 0, 0);
    }
}