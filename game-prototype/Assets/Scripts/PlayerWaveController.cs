using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlayerWaveController : MonoBehaviour
{
    [Tooltip("0 for Player 1, 1 for Player 2, 2 for non-playable background wave")]
    public int playerIndex;

    // This public flag allows other scripts (like the GameManager) to freeze this wave.
    public bool isUpdating = true;

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
    public int points = 100;
    public float amplitude = 1f;
    public Vector2 xLimits = new Vector2(-5, 5);
    public float movementSpeed = 1f;

    public float Frequency { get; private set; }

    private LineRenderer lineRenderer;
    private ControllerInput controller;
    private long lastEncoderCount = 0;
    private float perlinSeed;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = points;
    }

    void Start()
    {
        if (playerIndex == 2)
        {
            Frequency = frequency;
        }
        else
        {
            Frequency = initialFrequency;
            perlinSeed = Random.Range(0f, 100f);

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
        // If the wave is frozen, stop all updates immediately.
        if (!isUpdating) return;

        if (playerIndex != 2)
        {
            UpdateFrequencyForPlayer();
        }

        DrawWave();
    }

    void UpdateFrequencyForPlayer()
    {
        float totalChange = 0f;

        if (controller != null)
        {
            long encoderDelta = controller.EncoderCount - lastEncoderCount;
            lastEncoderCount = controller.EncoderCount;
            totalChange += encoderDelta * encoderSensitivity;
        }
        else
        {
            string axisName = (playerIndex == 0) ? "Horizontal_P1" : "Horizontal_P2";
            totalChange += Input.GetAxis(axisName) * keyboardSensitivity * Time.deltaTime;
        }

        Frequency += totalChange;
        Frequency = Mathf.Clamp(Frequency, 0.5f, 5f);
    }

    void DrawWave()
    {
        float tau = 2 * Mathf.PI;

        for (int i = 0; i < points; i++)
        {
            float progress = (float)i / (points - 1);
            float x = Mathf.Lerp(xLimits.x, xLimits.y, progress);
            float y = amplitude * Mathf.Sin((tau * Frequency * x) + (Time.time * movementSpeed));
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}