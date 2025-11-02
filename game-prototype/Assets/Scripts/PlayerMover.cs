using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [Tooltip("0 for Player 1, 1 for Player 2")]
    public int playerIndex;
    public bool canMove = true; 

    [Header("Control Sensitivity")]
    public float keyboardSensitivity = 5f;
    public float encoderSensitivity = 0.1f;
    private ControllerInput controller;

    // Stores the encoder value from the previous frame to calculate the change.
    private long lastEncoderCount;

    void Start()
    {
        // Get the controller assigned to this player from the HardwareManager.
        if (HardwareManager.Instance != null)
        {
            controller = HardwareManager.Instance.GetController(playerIndex);
        }

        if (controller != null)
        {
            // Initialize the encoder count for the first frame's calculation.
            lastEncoderCount = controller.EncoderCount;
        }
        else
        {
            // Log an error if no controller can be found, which helps with debugging.
            Debug.LogError($"PlayerMover for player {playerIndex} could not find its controller!");
        }
    }

    void Update()
    {
        // If canMove is false, stop all execution for this frame.
        if (!canMove)
        {
            return;
        }
        
        float movement = 0f;

        // Check if a controller was successfully assigned and if it's a real, connected piece of hardware.
        if (controller != null && controller.IsHardwareConnected)
        {
            // Use the encoder input from the hardware controller
            // Calculate how much the encoder has turned since the last frame.
            long encoderDelta = controller.EncoderCount - lastEncoderCount;
            lastEncoderCount = controller.EncoderCount;
            
            // The final movement is the change in encoder value multiplied by sensitivity.
            movement = encoderDelta * encoderSensitivity;
        }
        else
        {
            // If no controller is connected, fall back to keyboard input
            // Determine which input axis to use based on the player index.
            string axisName = (playerIndex == 0) ? "Horizontal_P1" : "Horizontal_P2";
            movement = Input.GetAxis(axisName) * keyboardSensitivity * Time.deltaTime;
        }

        // Apply the calculated movement to the character.
        if (movement != 0)
        {
            transform.Translate(movement, 0, 0);
        }
    }
}