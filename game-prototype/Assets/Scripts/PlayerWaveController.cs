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
    private long lastEncoderCount = 0;

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
                lastEncoderCount = controller.EncoderCount;
            }
        }
    }

    void Update()
    {
        if (!isUpdating) return;
        if (playerIndex != 2)
        {
            UpdateFrequencyForPlayer();
        }
        DrawWave();
    }

    void UpdateFrequencyForPlayer()
    {
        if (controller == null) return;

        float totalChange = 0f;

        // --- NEW, FOOLPROOF LOGIC ---
        // If a real hardware controller is connected and working, use its encoder data.
        if (controller.IsHardwareConnected)
        {
            long encoderDelta = controller.EncoderCount - lastEncoderCount;
            lastEncoderCount = controller.EncoderCount;
            totalChange = encoderDelta * encoderSensitivity;
        }
        // Otherwise, fall back to simple keyboard input, just like PlayerMover.cs.
        else
        {
            string axisName = (playerIndex == 0) ? "Horizontal_P1" : "Horizontal_P2";
            totalChange = Input.GetAxis(axisName) * keyboardSensitivity * Time.deltaTime;
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