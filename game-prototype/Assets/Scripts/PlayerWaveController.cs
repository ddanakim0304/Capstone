using UnityEngine;

[RequireComponent(typeof(LineRenderer))] // Automatically adds a LineRenderer if one doesn't exist
public class PlayerWaveController : MonoBehaviour
{
    [Tooltip("0 for Player 1, 1 for Player 2, 2 for non-playable background wave")]
    public int playerIndex;

    // --- Settings for Playable Waves (Index 0 & 1) ---
    [Header("Player Control & Instability")]
    [Tooltip("The starting frequency for playable characters.")]
    public float initialFrequency = 2f;
    public float keyboardSensitivity = 1f;
    public float encoderSensitivity = 0.1f;

    // --- Settings for Non-Playable Wave (Index 2) ---
    [Header("Background Wave Settings (Index 2)")]
    [Tooltip("The fixed frequency for the non-playable background wave.")]
    public float frequency = 2.5f;

    // --- General Wave Shape Settings ---
    [Header("Wave Shape")]
    public int points = 100; // Resolution of the line
    public float amplitude = 1f;
    public Vector2 xLimits = new Vector2(-5, 5); // Start and end X position of the wave
    public float movementSpeed = 1f; // How fast the wave animates

    // This is the public property the GameManager will check for a match
    public float Frequency { get; private set; }

    private LineRenderer lineRenderer;
    private ControllerInput controller;
    private long lastEncoderCount = 0;
    private float perlinSeed;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = points; // Set the number of points once
    }

    void Start()
    {
        // --- BEHAVIOR SWITCH BASED ON PLAYER INDEX ---

        if (playerIndex == 2)
        {
            // This is the non-playable background wave.
            // It uses the direct 'frequency' value and has no instability.
            Frequency = frequency;
        }
        else // This is a playable character (Index 0 or 1)
        {
            Frequency = initialFrequency;
            perlinSeed = Random.Range(0f, 100f);

            // Set up hardware/keyboard controls only for playable characters
            if (HardwareManager.Instance != null)
            {
                controller = HardwareManager.Instance.GetController(playerIndex);
            }

            if (controller == null)
            {
                Debug.Log($"Player {playerIndex}: No hardware controller found. Using keyboard input.");
            }
            else
            {
                lastEncoderCount = controller.EncoderCount;
            }
        }
    }

    void Update()
    {
        // Only playable characters need their frequency updated each frame
        if (playerIndex != 2)
        {
            UpdateFrequencyForPlayer();
        }

        DrawWave();
    }

    void UpdateFrequencyForPlayer()
    {
        float totalChange = 0f;

        // --- Calculate Input ---
        if (controller != null)
        {
            long encoderDelta = controller.EncoderCount - lastEncoderCount;
            lastEncoderCount = controller.EncoderCount;
            totalChange += encoderDelta * encoderSensitivity;
        }
        else // Fallback to keyboard
        {
            string axisName = (playerIndex == 0) ? "Horizontal_P1" : "Horizontal_P2";
            totalChange += Input.GetAxis(axisName) * keyboardSensitivity * Time.deltaTime;
        }

        // --- Apply Changes ---
        Frequency += totalChange;
        Frequency = Mathf.Clamp(Frequency, 0.5f, 5f); // Set reasonable limits for frequency
    }

    void DrawWave()
    {
        float tau = 2 * Mathf.PI;

        for (int i = 0; i < points; i++)
        {
            float progress = (float)i / (points - 1);
            float x = Mathf.Lerp(xLimits.x, xLimits.y, progress);

            // The sine wave calculation remains the same for all wave types
            float y = amplitude * Mathf.Sin((tau * Frequency * x) + (Time.time * movementSpeed));

            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}