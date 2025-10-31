using UnityEngine;

[RequireComponent(typeof(LineRenderer))] // Automatically adds a LineRenderer if one doesn't exist
public class PlayerWaveController : MonoBehaviour
{
    [Tooltip("0 for Player 1 (A/D), 1 for Player 2 (Left/Right)")]
    public int playerIndex;

    [Header("Wave Shape")]
    public int points = 100; // Resolution of the line
    public float amplitude = 1f;
    public Vector2 xLimits = new Vector2(-5, 5); // Start and end X position of the wave
    public float movementSpeed = 1f; // How fast the wave animates

    [Header("Control & Instability")]
    public float frequency = 2f; // Fixed frequency for in-game debug
    public float keyboardSensitivity = 1f;
    public float encoderSensitivity = 0.1f;
    public float driftSpeed = 0.1f;
    public float driftMagnitude = 0.2f;

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
        if (playerIndex == 2)
        {
            Frequency = frequency; // Set fixed frequency for non-playable background wave
            return;
        }

        if (HardwareManager.Instance != null)
        {
            controller = HardwareManager.Instance.GetController(playerIndex);
        }
        if (controller == null)
        {
            Debug.Log($"Player {playerIndex}: No hardware. Using keyboard.");
        }
        else
        {
            lastEncoderCount = controller.EncoderCount;
        }

        Frequency = frequency;
        perlinSeed = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (playerIndex != 2) // Skip input updates for non-playable background wave
        {
            UpdateFrequency();
        }
        DrawWave();
    }

    void UpdateFrequency()
    {
        float totalChange = 0f;

        // --- Calculate Input ---
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

        // --- Calculate Instability Drift ---
        float driftNoise = (Mathf.PerlinNoise(Time.time * driftSpeed, perlinSeed) * 2f - 1f);
        float driftChange = driftNoise * driftMagnitude * Time.deltaTime;

        // --- Apply Changes ---
        Frequency += totalChange + driftChange;
        Frequency = Mathf.Clamp(Frequency, 0.5f, 5f); // Set reasonable limits for frequency
    }

    void DrawWave()
    {
        float tau = 2 * Mathf.PI;

        for (int i = 0; i < points; i++)
        {
            float progress = (float)i / (points - 1);
            float x = Mathf.Lerp(xLimits.x, xLimits.y, progress);
            
            // This is the corrected sine wave calculation
            float y = amplitude * Mathf.Sin((tau * Frequency * x) + (Time.time * movementSpeed));

            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}