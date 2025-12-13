using UnityEngine;
using System.Collections;

public class AlarmClockGameManager : MiniGameManager
{
    [System.Serializable]
    public class PlayerAlarmSetup
    {
        [Header("Object References")]
        [Tooltip("The Transform of this player's hand sprite.")]
        public Transform handTransform;
        [Tooltip("The GameObject with this player's 'alarm on' visuals.")]
        public GameObject alarmOnSprite;
        [Tooltip("The GameObject with this player's 'alarm off' visuals.")]
        public GameObject alarmOffSprite;
        [Tooltip("The AudioSource that plays this player's unique alarm sound.")]
        public AudioSource alarmAudioSource;
        
        [Header("Hand Positions")]
        [Tooltip("The starting Y position of this player's hand (top).")]
        public float handStartPositionY = 1.5f;
        [Tooltip("The Y position the hand must pass to trigger the win. This is the actual finish line.")]
        public float winZonePositionY = -0.3f;
        [Tooltip("The final resting Y position of the hand after the win is triggered. Creates a follow-through effect.")]
        public float handEndPositionY = -0.5f;

        [Header("Individual Mechanics")]
        [Tooltip("How far down (in world units) the hand's target position moves with a single press.")]
        public float pressDistance = 0.2f;
        [Tooltip("How fast the hand's target position moves up per second (in world units) when idle.")]
        public float resistanceSpeed = 0.5f;
    }

    [Header("Player 1 Setup")]
    public PlayerAlarmSetup player1Alarm;

    [Header("Player 2 Setup")]
    public PlayerAlarmSetup player2Alarm;

    [Header("Global Game Settings")]
    [Tooltip("Controls how smoothly the hand moves. Higher values = faster/snappier.")]
    public float handMovementSmoothing = 8f;
    [Tooltip("How long to wait after both alarms are off before changing scenes.")]
    public float delayAfterWin = 1.5f;

    // --- Private Variables ---
    private ControllerInput p1_controller;
    private ControllerInput p2_controller;

    // These now track the target Y-position of the hand directly.
    private float p1_targetHandY;
    private float p2_targetHandY;

    private bool isP1AlarmOff = false;
    private bool isP2AlarmOff = false;

    void Start()
    {
        if (HardwareManager.Instance != null)
        {
            p1_controller = HardwareManager.Instance.GetController(0);
            p2_controller = HardwareManager.Instance.GetController(1);
        }
        
        InitializePlayerAlarm(player1Alarm, out p1_targetHandY);
        InitializePlayerAlarm(player2Alarm, out p2_targetHandY);
    }

    void Update()
    {
        if (isGameWon) return;

        // --- Handle Player 1 ---
        HandlePlayerLogic(player1Alarm, ref p1_targetHandY, ref isP1AlarmOff, p1_controller);
        
        // --- Handle Player 2 ---
        HandlePlayerLogic(player2Alarm, ref p2_targetHandY, ref isP2AlarmOff, p2_controller);
    }

    // A single, reusable function to handle all logic for one player.
    private void HandlePlayerLogic(PlayerAlarmSetup alarmSetup, ref float targetHandY, ref bool isAlarmOff, ControllerInput controller)
    {
        if (isAlarmOff) return;

        // Update the target hand position based on input.
        bool playerPressed = controller != null && controller.IsButtonPressed;
        targetHandY = UpdateTargetHandPosition(targetHandY, playerPressed, alarmSetup);
        
        // Always move the visual hand smoothly towards its target.
        UpdateHandVisuals(alarmSetup, targetHandY);

        // The win condition is a direct check of the hand's visual position.
        if (alarmSetup.handTransform.position.y <= alarmSetup.winZonePositionY)
        {
            isAlarmOff = true;
            DisableAlarm(alarmSetup); 
            CheckWinCondition();
        }
    }

    // Calculates the new target Y-position for the hand.
    private float UpdateTargetHandPosition(float currentTargetY, bool isPressed, PlayerAlarmSetup alarmSetup)
    {
        if (isPressed)
        {
            // Move the target position down by the specified distance.
            currentTargetY -= alarmSetup.pressDistance;
        }
        else
        {
            // Move the target position up over time by the resistance speed.
            currentTargetY += alarmSetup.resistanceSpeed * Time.deltaTime;
        }
        
        // Enforce a ceiling so the target can't go above the start position
        return Mathf.Min(currentTargetY, alarmSetup.handStartPositionY);
    }
    
    // Smoothly moves the hand Transform towards its target Y position.
    private void UpdateHandVisuals(PlayerAlarmSetup alarmSetup, float targetY)
    {
        if (alarmSetup.handTransform == null) return;
        
        float currentY = alarmSetup.handTransform.position.y;
        float newY = Mathf.Lerp(currentY, targetY, handMovementSmoothing * Time.deltaTime);
        
        alarmSetup.handTransform.position = new Vector3(alarmSetup.handTransform.position.x, newY, alarmSetup.handTransform.position.z);
    }

    private void InitializePlayerAlarm(PlayerAlarmSetup alarmSetup, out float targetY)
    {
        targetY = alarmSetup.handStartPositionY;

        if (alarmSetup.alarmOnSprite != null) alarmSetup.alarmOnSprite.SetActive(true);
        if (alarmSetup.alarmOffSprite != null) alarmSetup.alarmOffSprite.SetActive(false);
        
        if(alarmSetup.handTransform != null)
        {
            alarmSetup.handTransform.position = new Vector3(alarmSetup.handTransform.position.x, alarmSetup.handStartPositionY, alarmSetup.handTransform.position.z);
        }

        if (alarmSetup.alarmAudioSource != null)
        {
            alarmSetup.alarmAudioSource.loop = true;
            alarmSetup.alarmAudioSource.Play();
        }
    }

    private void DisableAlarm(PlayerAlarmSetup alarmSetup)
    {
        if (alarmSetup.alarmOnSprite != null) alarmSetup.alarmOnSprite.SetActive(false);
        if (alarmSetup.alarmOffSprite != null) alarmSetup.alarmOffSprite.SetActive(true);
        
        // Snap hand to its absolute final position to ensure it's perfectly aligned.
        if (alarmSetup.handTransform != null)
        {
             alarmSetup.handTransform.position = new Vector3(alarmSetup.handTransform.position.x, alarmSetup.handEndPositionY, alarmSetup.handTransform.position.z);
        }

        if (alarmSetup.alarmAudioSource != null)
        {
            alarmSetup.alarmAudioSource.Stop();
        }
    }

    private void CheckWinCondition()
    {
        if (isP1AlarmOff && isP2AlarmOff && !isGameWon)
        {
            StartCoroutine(WinSequence());
        }
    }

    private IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(delayAfterWin);
        WinGame();
    }
}