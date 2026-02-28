using UnityEngine;
using System.Collections;
[RequireComponent(typeof(Rigidbody2D))]
public class CarController : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────────
    [Header("Movement Physics")]
    public float movePower    = 30f;   // Horizontal force per frame
    public float maxSpeed     = 6f;    // Horizontal speed cap
    public float jumpPower    = 12f;   // Jump impulse magnitude

    [Header("Input")]
    [Tooltip("Multiplier applied to encoder count delta for horizontal force.")]
    public float encoderSensitivity = 0.1f;

    [Tooltip("How long (seconds) a detected encoder turn keeps applying force. Compensates for infrequent UDP packets.")]
    public float encoderHoldDuration = 0.5f;

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
    public Color p1Color      = new Color(1f, 0.4f, 0.4f);
    public Color p2Color      = new Color(0.4f, 1f, 0.4f);
    public Color agreedColor  = new Color(0.2f, 0.6f, 1f);
    public Color neutralColor = Color.white;

    [Header("Arrival Fade")]
    [Tooltip("Seconds over which all three button sprites fade to transparent when the car stops.")]
    public float buttonFadeDuration = 0.5f;
    [Tooltip("Seconds over which the car's own SpriteRenderers fade out (triggered by FinalCutsceneMiniGame).")]
    public float carFadeDuration = 1f;

    // ── Audio ─────────────────────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("Looping engine sound played while the car moves left or right.")]
    public AudioClip engineClip;
    [Tooltip("One-shot sound played each time the car jumps.")]
    public AudioClip jumpClip;
    [Range(0f, 1f)]
    public float engineVolume = 1f;
    [Range(0f, 1f)]
    public float jumpVolume   = 1f;
    [Tooltip("Seconds for the engine to fade in / fade out.")]
    public float engineFadeTime = 0.25f;

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

    // Per-player encoder tracking + hold buffer for horizontal force
    private long  lastEncoderCount0 = 0;
    private long  lastEncoderCount1 = 0;
    private float encoderHoldTimer0 = 0f;  // how long to keep applying P1 encoder force
    private float encoderHoldTimer1 = 0f;
    private float encoderHoldDir0   = 0f;  // last detected encoder direction for P1 (+1 / -1)
    private float encoderHoldDir1   = 0f;

    // Engine has its own AudioSource so it never conflicts with jump (AudioManager.Center)
    private AudioSource  engineSource        = null;
    private Coroutine    engineFadeCoroutine = null;
    private bool         isEngineRunning     = false;

    // Stuck detection
    private float stuckTimer     = 0f;
    private bool  wantsToMove    = false;

    // Cutscene lock – set by StopCar(); disables all player input
    private bool  isStopped      = false;

    // Horizontal force intent written by Update(), consumed by FixedUpdate()
    private float pendingHorizontalForce = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Set up a dedicated AudioSource for the engine loop
        // This keeps it on a completely separate channel from AudioManager's sfxCenterSource
        // so jump one-shots can play simultaneously without interrupting the loop.
        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.clip        = engineClip;
        engineSource.loop        = true;
        engineSource.playOnAwake = false;
        engineSource.volume      = 0f;
        engineSource.spatialBlend = 0f; // 2-D sound

        if (HardwareManager.Instance != null)
        {
            controller0 = HardwareManager.Instance.GetController(0);
            controller1 = HardwareManager.Instance.GetController(1);
        }
        else
        {
            Debug.LogWarning("[CarController] HardwareManager not found – using keyboard fallback only.");
        }

        // Sync encoder baselines so there's no jump on first frame
        if (controller0 != null) lastEncoderCount0 = controller0.EncoderCount;
        if (controller1 != null) lastEncoderCount1 = controller1.EncoderCount;
    }
    public void StopCar()
    {
        isStopped = true;
        rb.linearVelocity = Vector2.zero;
        StopEngine();
        // Fade out the UI buttons immediately
        StartCoroutine(FadeOutButtons(buttonFadeDuration));
        Debug.Log("[CarController] Car stopped for cutscene.");
    }
    private IEnumerator FadeOutButtons(float duration)
    {
        SpriteRenderer[] buttons = { leftButtonSprite, rightButtonSprite, jumpButtonSprite };
        float[] startAlphas = new float[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
            startAlphas[i] = buttons[i] != null ? buttons[i].color.a : 0f;

        float elapsed = 0f;
        duration = Mathf.Max(duration, 0.01f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                Color c = buttons[i].color;
                c.a = Mathf.Lerp(startAlphas[i], 0f, t);
                buttons[i].color = c;
            }
            yield return null;
        }
        foreach (SpriteRenderer sr in buttons)
        {
            if (sr == null) continue;
            Color c = sr.color; c.a = 0f; sr.color = c;
        }
    }
    public IEnumerator FadeOutCar(float duration)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        float[] startAlphas = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startAlphas[i] = renderers[i].color.a;

        float elapsed = 0f;
        duration = Mathf.Max(duration, 0.01f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = renderers[i].color;
                c.a = Mathf.Lerp(startAlphas[i], 0f, t);
                renderers[i].color = c;
            }
            yield return null;
        }
        foreach (SpriteRenderer sr in renderers)
        {
            Color c = sr.color; c.a = 0f; sr.color = c;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        // Cutscene lock: ignore all input while stopped
        if (isStopped) return;

        // ── Tick down jump buffers ─────────────────────────────────────────
        jumpBuffer0 = Mathf.Max(0f, jumpBuffer0 - Time.deltaTime);
        jumpBuffer1 = Mathf.Max(0f, jumpBuffer1 - Time.deltaTime);

        // ── Sample jump actions this frame ────────────────────────────────
        CarAction action0 = GetJumpActionForPlayer(0, controller0, prevBtn0);
        CarAction action1 = GetJumpActionForPlayer(1, controller1, prevBtn1);

        // If a player pressed jump this frame, fill their buffer
        if (action0 == CarAction.Jump) jumpBuffer0 = jumpBufferTime;
        if (action1 == CarAction.Jump) jumpBuffer1 = jumpBufferTime;

        // Update previous-frame button states
        prevBtn0 = GetRawButton(0, controller0);
        prevBtn1 = GetRawButton(1, controller1);

        // ── Resolve jump ───────────────────────────────────────────────────
        bool bothWantJump = jumpBuffer0 > 0f && jumpBuffer1 > 0f;
        if (bothWantJump && isGrounded)
        {
            ExecuteAction(CarAction.Jump);
            jumpBuffer0 = 0f;
            jumpBuffer1 = 0f;
        }

        // ── Continuous horizontal force (like PlayerMover) ─────────────────
        // Each player produces a force value from encoder count delta + keyboard.
        // If both agree on direction (same sign), apply force to the car.
        float force0 = GetPlayerHorizontalForce(0, controller0, ref lastEncoderCount0, ref encoderHoldTimer0, ref encoderHoldDir0);
        float force1 = GetPlayerHorizontalForce(1, controller1, ref lastEncoderCount1, ref encoderHoldTimer1, ref encoderHoldDir1);

        bool bothMoveRight = force0 > 0f && force1 > 0f;
        bool bothMoveLeft  = force0 < 0f && force1 < 0f;
        bool carMoving     = bothMoveRight || bothMoveLeft;

        if (bothMoveRight)
            pendingHorizontalForce = movePower;
        else if (bothMoveLeft)
            pendingHorizontalForce = -movePower;
        else
            pendingHorizontalForce = 0f;

        // ── Engine audio ───────────────────────────────────────────────────
        if (carMoving && !isEngineRunning)
            StartEngine();
        else if (!carMoving && isEngineRunning)
            StopEngine();

        // ── Button display ─────────────────────────────────────────────────
        UpdateButtonDisplay(leftButtonSprite,  force0 < 0f, force1 < 0f);
        UpdateButtonDisplay(rightButtonSprite, force0 > 0f, force1 > 0f);
        UpdateButtonDisplay(jumpButtonSprite,  GetJumpHeld(0, controller0), GetJumpHeld(1, controller1));

        // ── Stuck recovery ─────────────────────────────────────────────────
        wantsToMove = carMoving;

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
        // Keep the car frozen when stopped
        if (isStopped)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Drive horizontal velocity directly – bypasses ground friction and is frame-rate independent
        if (pendingHorizontalForce != 0f)
        {
            float targetX = Mathf.Sign(pendingHorizontalForce) * maxSpeed;
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, movePower * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

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
    private CarAction GetJumpActionForPlayer(int idx, ControllerInput ctrl, bool prevButton)
    {
        // Hardware rising edge
        if (ctrl != null && ctrl.IsHardwareConnected)
        {
            bool btnNow    = ctrl.IsButtonPressed;
            bool btnRising = btnNow && !prevButton;
            if (btnRising) return CarAction.Jump;
        }

        // Keyboard always accepted alongside hardware
        if (idx == 0 && Input.GetKeyDown(KeyCode.W))        return CarAction.Jump;
        if (idx == 1 && Input.GetKeyDown(KeyCode.UpArrow))  return CarAction.Jump;

        return CarAction.None;
    }
    private float GetPlayerHorizontalForce(int idx, ControllerInput ctrl,
        ref long lastCount, ref float holdTimer, ref float holdDir)
    {
        // ── Hardware: refresh hold buffer on each new encoder delta ──
        if (ctrl != null && ctrl.IsHardwareConnected)
        {
            long currentCount = ctrl.EncoderCount;
            long delta        = lastCount - currentCount;
            lastCount         = currentCount;

            if (delta > 0)
            {
                holdDir   =  1f;
                holdTimer = encoderHoldDuration;
            }
            else if (delta < 0)
            {
                holdDir   = -1f;
                holdTimer = encoderHoldDuration;
            }
        }

        // Tick the hold timer down
        holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime);

        float force = 0f;

        // Apply the buffered encoder direction while the timer is alive
        if (holdTimer > 0f)
            force += holdDir;

        // Keyboard always accepted alongside hardware (for debugging)
        if (idx == 0)
        {
            if (Input.GetKey(KeyCode.D))      force += 1f;
            else if (Input.GetKey(KeyCode.A)) force -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.RightArrow))     force += 1f;
            else if (Input.GetKey(KeyCode.LeftArrow)) force -= 1f;
        }

        return force;
    }
    private bool GetRawButton(int idx, ControllerInput ctrl)
    {
        if (ctrl != null && ctrl.IsHardwareConnected && ctrl.IsButtonPressed) return true;
        if (idx == 0) return Input.GetKey(KeyCode.W);
        return Input.GetKey(KeyCode.UpArrow);
    }
   private bool GetJumpHeld(int idx, ControllerInput ctrl)
    {
        if (ctrl != null && ctrl.IsHardwareConnected && ctrl.IsButtonPressed) return true;
        if (idx == 0) return Input.GetKey(KeyCode.W);
        return Input.GetKey(KeyCode.UpArrow);
    }
    private void ExecuteAction(CarAction action)
    {
        if (action == CarAction.Jump && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // consistent jump height
            rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            isGrounded = false;

            if (jumpClip != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(jumpClip, AudioPan.Center, jumpVolume);
        }
    }

    // ── Engine audio helpers ───────────────────────────────────────────────────

    private void StartEngine()
    {
        if (engineClip == null || engineSource == null) return;
        isEngineRunning = true;
        if (engineFadeCoroutine != null) StopCoroutine(engineFadeCoroutine);
        engineSource.clip = engineClip;
        if (!engineSource.isPlaying) engineSource.Play();
        engineFadeCoroutine = StartCoroutine(FadeEngine(engineSource.volume, engineVolume));
    }

    private void StopEngine()
    {
        if (engineSource == null) return;
        isEngineRunning = false;
        if (engineFadeCoroutine != null) StopCoroutine(engineFadeCoroutine);
        engineFadeCoroutine = StartCoroutine(FadeEngine(engineSource.volume, 0f, stopWhenDone: true));
    }

    private System.Collections.IEnumerator FadeEngine(float from, float to, bool stopWhenDone = false)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(engineFadeTime, 0.01f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            engineSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        engineSource.volume = to;
        if (stopWhenDone) engineSource.Stop();
        engineFadeCoroutine = null;
    }
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

    private void OnDestroy()
    {
        // Immediately silence the engine source (coroutines won't run after destroy)
        if (engineSource != null)
        {
            engineSource.Stop();
        }
    }
}
