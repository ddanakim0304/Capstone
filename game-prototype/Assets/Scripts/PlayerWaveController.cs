using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlayerWaveController : MonoBehaviour
{
    [Tooltip("0 for Player 1, 1 for Player 2, 2 for non-playable background wave")]
    public int playerIndex;
    public bool isUpdating = true;

    [Header("Player Control & Instability")]
    public float initialFrequency = 2f;
    public float keyboardSensitivity = 1f;
    public float encoderSensitivity = 0.1f;

    [Header("Background Wave Settings (Index 2)")]
    public float frequency = 2.5f;

    [Header("Wave Shape")]
    public int points = 100;
    public float amplitude = 1f;
    public Vector2 xLimits = new Vector2(-5, 5);
    public float movementSpeed = 1f;
    public float Frequency { get; private set; }

    private LineRenderer lineRenderer;
    private ControllerInput controller;
    // Stores the encoder value from the previous frame to calculate the delta.
    private long lastEncoderCount = 0;

    void Awake()
    {
        // Get a reference to the LineRenderer component attached to this GameObject.
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = points;
    }

    void Start()
    {
        // If this is a background wave, use its fixed frequency setting.
        if (playerIndex == 2)
        {
            Frequency = frequency;
        }
        // Otherwise, it's a player-controlled wave.
        else
        {
            Frequency = initialFrequency;
            // Attempt to get the assigned controller from the HardwareManager.
            if (HardwareManager.Instance != null)
            {
                controller = HardwareManager.Instance.GetController(playerIndex);
            }

            if (controller == null)
            {
                Debug.LogError($"Player {playerIndex}: Controller could not be assigned!");
            }
            else
            {
                // Initialize the encoder count for the first frame's calculation.
                lastEncoderCount = controller.EncoderCount;
            }
        }
    }

    void Update()
    {
        if (!isUpdating) return;
        
        // Only players' frequencies can be changed at runtime.
        if (playerIndex != 2)
        {
            UpdateFrequencyForPlayer();
        }
        DrawWave();
    }

    // Adjusts the wave's frequency based on hardware or keyboard input.
    void UpdateFrequencyForPlayer()
    {
        if (controller == null) return;

        float totalChange = 0f;

        // Prioritize physical hardware input if a controller is connected.
        if (controller.IsHardwareConnected)
        {
            // Calculate how much the encoder has turned since the last frame.
            long encoderDelta = controller.EncoderCount - lastEncoderCount;
            lastEncoderCount = controller.EncoderCount;
            totalChange = encoderDelta * encoderSensitivity;
        }
        // If no hardware is found, fall back to keyboard input.
        else
        {
            string axisName = (playerIndex == 0) ? "Horizontal_P1" : "Horizontal_P2";
            totalChange = Input.GetAxis(axisName) * keyboardSensitivity * Time.deltaTime;
        }

        // Apply the input change to the frequency and keep it within a playable range.
        Frequency += totalChange;
        Frequency = Mathf.Clamp(Frequency, 0.5f, 5f);
    }

    // Calculates and sets the positions of the LineRenderer's points to form a sine wave.
    void DrawWave()
    {
        float tau = 2 * Mathf.PI;
        for (int i = 0; i < points; i++)
        {
            float progress = (float)i / (points - 1);
            float x = Mathf.Lerp(xLimits.x, xLimits.y, progress);
            // The y-position is determined by a sine function, creating the wave shape.
            float y = amplitude * Mathf.Sin((tau * Frequency * x) + (Time.time * movementSpeed));
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}