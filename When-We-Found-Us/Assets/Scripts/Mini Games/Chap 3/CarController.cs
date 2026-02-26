using UnityEngine;

/// <summary>
/// Picopark-style dual-input car controller.
///
/// HOW SIMULTANEOUS INPUT WORKS
/// ─────────────────────────────
/// Every frame each player reports their desired action (Left / Right / Jump / None).
/// The car only responds when BOTH players choose the SAME non-None action that
/// frame.  The on-screen button sprites are tinted to show each player's intent:
///   • Only P1 pressing  → P1 colour
///   • Only P2 pressing  → P2 colour
///   • Both pressing same → blue (agreed colour)
///   • Nobody pressing   → neutral colour
///
/// HARDWARE INPUT MAPPING (per ControllerInput)
/// ─────────────────────────────────────────────
///   Left   = encoder delta < -encoderThreshold
///   Right  = encoder delta >  encoderThreshold
///   Jump   = button just pressed (rising edge)
///
/// KEYBOARD FALLBACK
/// ──────────────────
///   P1 : A / D / Space
///   P2 : ← / → / Enter
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CarController : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────────
    [Header("Movement Physics")]
    public float movePower    = 30f;   // Horizontal force per frame
    public float maxSpeed     = 6f;    // Horizontal speed cap
    public float jumpPower    = 12f;   // Jump impulse magnitude

    [Header("Input")]
    [Tooltip("Encoder delta magnitude required to register Left/Right intent.")]
    public float encoderThreshold = 0.5f;

    [Tooltip("How long (seconds) a jump press is remembered, giving both players time to press together.")]
    public float jumpBufferTime = 0.3f;

    // ── Button Display ────────────────────────────────────────────────────────
    [Header("Button Sprite Renderers (optional)")]
    [Tooltip("SpriteRenderer for the Left button in the scene.")]
    public SpriteRenderer leftButtonSprite;
    [Tooltip("SpriteRenderer for the Right button in the scene.")]
    public SpriteRenderer rightButtonSprite;
    [Tooltip("SpriteRenderer for the Jump/Up button in the scene.")]
    public SpriteRenderer jumpButtonSprite;

    [Header("Button Colours")]
    public Color p1Color      = new Color(1f, 0.4f, 0.4f);   // Player 1 (red-ish)
    public Color p2Color      = new Color(0.4f, 1f, 0.4f);   // Player 2 (green-ish)
    public Color agreedColor  = new Color(0.2f, 0.6f, 1f);   // Both agreed (blue)
    public Color neutralColor = Color.white;

    // ── Stuck Recovery ────────────────────────────────────────────────────────
    [Header("Stuck-on-Rock Recovery")]
    [Tooltip("If the car tries to move horizontally but barely moves, apply a small upward nudge.")]
    public float stuckCheckDuration  = 0.4f;  // Seconds to watch for being stuck
    public float stuckSpeedThreshold = 0.1f;  // X-speed below which we consider 'stuck'
    public float unstuckUpForce      = 4f;    // Upward impulse to escape a rock

    // ── Private ───────────────────────────────────────────────────────────────
    private enum CarAction { None, Left, Right, Jump }

    private Rigidbody2D   rb;
    private ControllerInput controller0;   // Player 1
    private ControllerInput controller1;   // Player 2

    private bool  isGrounded     = false;
    private bool  prevBtn0       = false;   // Previous-frame button state for P1
    private bool  prevBtn1       = false;   // Previous-frame button state for P2

    // Jump input buffers – keeps intent alive for jumpBufferTime seconds
    private float jumpBuffer0    = 0f;
    private float jumpBuffer1    = 0f;

    // Stuck detection
    private float stuckTimer     = 0f;
    private bool  wantsToMove    = false;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (HardwareManager.Instance != null)
        {
            controller0 = HardwareManager.Instance.GetController(0);
            controller1 = HardwareManager.Instance.GetController(1);
        }
        else
        {
            Debug.LogWarning("[CarController] HardwareManager not found – using keyboard fallback only.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        // ── Tick down jump buffers ─────────────────────────────────────────
        jumpBuffer0 = Mathf.Max(0f, jumpBuffer0 - Time.deltaTime);
        jumpBuffer1 = Mathf.Max(0f, jumpBuffer1 - Time.deltaTime);

        // ── Sample each player's action this frame ─────────────────────────
        CarAction action0 = GetActionForPlayer(0, controller0, prevBtn0);
        CarAction action1 = GetActionForPlayer(1, controller1, prevBtn1);

        // If a player pressed jump this frame, fill their buffer
        if (action0 == CarAction.Jump) jumpBuffer0 = jumpBufferTime;
        if (action1 == CarAction.Jump) jumpBuffer1 = jumpBufferTime;

        // Update previous-frame button states
        prevBtn0 = GetRawButton(0, controller0);
        prevBtn1 = GetRawButton(1, controller1);

        // ── Resolve agreed action ──────────────────────────────────────────
        // For Jump: use the buffers so both players don't need the exact same frame
        bool bothWantJump = jumpBuffer0 > 0f && jumpBuffer1 > 0f;
        if (bothWantJump && isGrounded)
        {
            ExecuteAction(CarAction.Jump);
            jumpBuffer0 = 0f;   // consume both buffers only when jump actually fires
            jumpBuffer1 = 0f;
        }
        else if (action0 != CarAction.None && action0 != CarAction.Jump &&
                 action0 == action1)
        {
            // Left / Right: require same action held simultaneously (unchanged)
            ExecuteAction(action0);
        }

        // ── Button display ─────────────────────────────────────────────────
        UpdateButtonDisplay(leftButtonSprite,  action0 == CarAction.Left,  action1 == CarAction.Left);
        UpdateButtonDisplay(rightButtonSprite, action0 == CarAction.Right, action1 == CarAction.Right);
        // Show jump colour for the full duration the button is physically held down
        UpdateButtonDisplay(jumpButtonSprite,  GetJumpHeld(0, controller0), GetJumpHeld(1, controller1));

        // ── Stuck recovery ─────────────────────────────────────────────────
        wantsToMove = (action0 == CarAction.Left  || action0 == CarAction.Right ||
                       action1 == CarAction.Left  || action1 == CarAction.Right);

        if (wantsToMove && !isGrounded == false)   // only check while on or near ground
        {
            if (Mathf.Abs(rb.linearVelocity.x) < stuckSpeedThreshold)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= stuckCheckDuration)
                {
                    stuckTimer = 0f;
                    rb.AddForce(Vector2.up * unstuckUpForce, ForceMode2D.Impulse);
                    Debug.Log("[CarController] Unstuck nudge applied.");
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void FixedUpdate()
    {
        // Clamp X to startPositionX (can't go left of the start)
        if (CarMiniGameManager.Instance != null)
        {
            float minX = CarMiniGameManager.Instance.startPositionX;
            if (rb.position.x < minX)
            {
                rb.position = new Vector2(minX, rb.position.y);
                if (rb.linearVelocity.x < 0)
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }

        // Clamp horizontal speed
        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns what action the given player wants this frame.</summary>
    private CarAction GetActionForPlayer(int idx, ControllerInput ctrl, bool prevButton)
    {
        // ── Hardware path ──
        if (ctrl != null && ctrl.IsHardwareConnected)
        {
            long delta    = ctrl.EncoderDelta;
            bool btnNow   = ctrl.IsButtonPressed;
            bool btnRising = btnNow && !prevButton;   // rising edge

            if (btnRising)             return CarAction.Jump;
            if (delta >  encoderThreshold) return CarAction.Right;
            if (delta < -encoderThreshold) return CarAction.Left;
            return CarAction.None;
        }

        // ── Keyboard fallback ──
        if (idx == 0)
        {
            bool jumpKey = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
            if (jumpKey)                         return CarAction.Jump;
            if (Input.GetKey(KeyCode.D))         return CarAction.Right;
            if (Input.GetKey(KeyCode.A))         return CarAction.Left;
        }
        else
        {
            bool jumpKey = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Return);
            if (jumpKey)                           return CarAction.Jump;
            if (Input.GetKey(KeyCode.RightArrow))  return CarAction.Right;
            if (Input.GetKey(KeyCode.LeftArrow))   return CarAction.Left;
        }

        return CarAction.None;
    }

    /// <summary>Returns whether the raw button (not edge-detected) is held.</summary>
    private bool GetRawButton(int idx, ControllerInput ctrl)
    {
        if (ctrl != null && ctrl.IsHardwareConnected)
            return ctrl.IsButtonPressed;

        if (idx == 0) return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space);
        return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Return);
    }

    /// <summary>Returns true for the entire duration the jump key/button is physically held.</summary>
    private bool GetJumpHeld(int idx, ControllerInput ctrl)
    {
        if (ctrl != null && ctrl.IsHardwareConnected)
            return ctrl.IsButtonPressed;

        if (idx == 0) return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space);
        return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Return);
    }

    /// <summary>Applies the agreed action to the car.</summary>
    private void ExecuteAction(CarAction action)
    {
        switch (action)
        {
            case CarAction.Left:
                rb.AddForce(Vector2.left * movePower);
                break;

            case CarAction.Right:
                rb.AddForce(Vector2.right * movePower);
                break;

            case CarAction.Jump:
                if (isGrounded)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // consistent jump height
                    rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                    isGrounded = false;
                }
                break;
        }
    }

    /// <summary>Tints a button sprite based on which players are pressing it.</summary>
    private void UpdateButtonDisplay(SpriteRenderer sprite, bool p1Pressing, bool p2Pressing)
    {
        if (sprite == null) return;

        if (p1Pressing && p2Pressing)
            sprite.color = agreedColor;
        else if (p1Pressing)
            sprite.color = p1Color;
        else if (p2Pressing)
            sprite.color = p2Color;
        else
            sprite.color = neutralColor;
    }

    // ── Ground Detection ──────────────────────────────────────────────────────
    // All three callbacks funnel into one method that queries every active contact
    // on the rigidbody, so being wedged between two rocks never incorrectly clears
    // isGrounded when one of the side contacts exits.
    private void OnCollisionEnter2D(Collision2D collision) => UpdateGroundedState();
    private void OnCollisionStay2D(Collision2D collision)  => UpdateGroundedState();
    private void OnCollisionExit2D(Collision2D collision)  => UpdateGroundedState();

    private void UpdateGroundedState()
    {
        ContactPoint2D[] contacts = new ContactPoint2D[16];
        int count = rb.GetContacts(contacts);
        isGrounded = false;
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }
}
