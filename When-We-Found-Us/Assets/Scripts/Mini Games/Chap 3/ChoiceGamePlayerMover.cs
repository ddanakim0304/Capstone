using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimePhysicsMover : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerIndex;

    [Header("Movement Physics")]
    public float movePower = 50f;
    public float maxSpeed = 8f;
    public float jumpPower = 10f;

    [Header("Input Sensitivity")]
    // Scaling factor for hardware encoder input
    public float encoderSensitivity = 2.0f;
    
    private Rigidbody2D rb;
    private ControllerInput controller;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();


        if (HardwareManager.Instance != null)
        {
            controller = HardwareManager.Instance.GetController(playerIndex);
        }
        else
        {
            Debug.LogError("HardwareManager not found in scene!");
        }
    }

    // Handle jump inputs every frame to ensure responsiveness
    void Update()
    {

        if (controller != null)
        {

            bool jumpPressed = false;

            if (controller.IsHardwareConnected)
            {
                if (controller.IsButtonPressed && isGrounded) 
                {
                    jumpPressed = true;
                }
            }


            if (playerIndex == 0 && Input.GetKeyDown(KeyCode.W)) jumpPressed = true;
            if (playerIndex == 1 && Input.GetKeyDown(KeyCode.UpArrow)) jumpPressed = true;

            if (jumpPressed && isGrounded)
            {
                Jump();
            }
        }
    }

    // Apply physics forces for movement
    // Physics-based movement logic
    void FixedUpdate()
    {
        float inputForce = 0f;

        if (controller != null && controller.IsHardwareConnected)
        {

            long delta = controller.EncoderDelta;
            inputForce += delta * encoderSensitivity;
        }



        if (playerIndex == 0)
        {
            if (Input.GetKey(KeyCode.D)) inputForce += 1f;
            else if (Input.GetKey(KeyCode.A)) inputForce -= 1f;
        }

        else if (playerIndex == 1)
        {
            if (Input.GetKey(KeyCode.RightArrow)) inputForce += 1f;
            else if (Input.GetKey(KeyCode.LeftArrow)) inputForce -= 1f;
        }


        if (Mathf.Abs(inputForce) > 0.01f)
        {

            rb.AddForce(Vector2.right * inputForce * movePower);
        }



        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y);
        }
    }

    // Applies upward force for jumping
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        isGrounded = false;
    }


    // Detects ground collision to reset jump capability


    private void OnCollisionEnter2D(Collision2D collision)
    {

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