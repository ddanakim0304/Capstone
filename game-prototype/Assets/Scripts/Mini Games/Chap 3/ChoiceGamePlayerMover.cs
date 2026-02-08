using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimePhysicsMover : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("0 for Player 1, 1 for Player 2")]
    public int playerIndex;

    [Header("Movement Physics")]
    public float movePower = 50f;     // Force applied when turning knob
    public float maxSpeed = 8f;       // Max movement speed
    public float jumpPower = 10f;     // Upward force for jump

    [Header("Input Sensitivity")]
    public float encoderSensitivity = 2.0f; // Multiplier for hardware knob
    
    private Rigidbody2D rb;
    private ControllerInput controller;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Find the specific controller for this player index
        if (HardwareManager.Instance != null)
        {
            controller = HardwareManager.Instance.GetController(playerIndex);
        }
        else
        {
            Debug.LogError("HardwareManager not found in scene!");
        }
    }

    void Update()
    {
        // We handle Jump in Update so we don't miss the button press frame
        if (controller != null)
        {
            // Check Hardware Button OR Keyboard Fallback for Jump
            bool jumpPressed = false;

            if (controller.IsHardwareConnected)
            {
                if (controller.IsButtonPressed && isGrounded) 
                {
                    jumpPressed = true;
                }
            }
            else
            {
                // FALLBACK: Raw Keyboard check (No InputManager)
                if (playerIndex == 0 && (Input.GetKeyDown(KeyCode.W))) jumpPressed = true;
                if (playerIndex == 1 && (Input.GetKeyDown(KeyCode.UpArrow) )) jumpPressed = true;
            }

            if (jumpPressed && isGrounded)
            {
                Jump();
            }
        }
    }

    void FixedUpdate()
    {
        float inputForce = 0f;

        // --- 1. GET INPUT ---
        if (controller != null && controller.IsHardwareConnected)
        {
            // HARDWARE: Use the Encoder Delta (Change in rotation since last frame)
            // If the player turns the knob fast, Delta is high. If stopped, Delta is 0.
            long delta = controller.EncoderDelta;
            
            // Apply sensitivity
            inputForce = delta * encoderSensitivity;
        }
        else
        {
            // FALLBACK: Raw Keyboard check (No InputManager)
            // Player 1: A / D
            if (playerIndex == 0)
            {
                if (Input.GetKey(KeyCode.D)) inputForce = 1f;
                else if (Input.GetKey(KeyCode.A)) inputForce = -1f;
            }
            // Player 2: Left / Right Arrows
            else if (playerIndex == 1)
            {
                if (Input.GetKey(KeyCode.RightArrow)) inputForce = 1f;
                else if (Input.GetKey(KeyCode.LeftArrow)) inputForce = -1f;
            }
        }

        // --- 2. APPLY PHYSICS FORCE ---
        if (Mathf.Abs(inputForce) > 0.01f)
        {
            // Add force in the X direction
            rb.AddForce(Vector2.right * inputForce * movePower);
        }

        // --- 3. LIMIT SPEED ---
        // Prevents the slime from accelerating infinitely
        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y);
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset Y velocity for consistent jump height
        rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        isGrounded = false;
    }

    // --- GROUND CHECK ---
    // Simple collision check to see if we can jump
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we hit something below us
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }
}