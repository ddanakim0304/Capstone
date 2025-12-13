using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BrushTeethGameManager : MiniGameManager
{
    // Public property for the Coordinator script to check
    public bool IsTaskFinished { get; private set; } = false;
    
    // To track direction for the finishing snap
    private float lastInputDelta = 0f;

    [System.Serializable]
    public class BubbleThreshold
    {
        [Tooltip("The bubble object to show.")]
        public GameObject bubbleObject;
        [Tooltip("How many full brush strokes (start->end) needed to unlock this bubble.")]
        public float brushesNeeded;
    }

    [Header("Setup")]
    [Tooltip("The actual toothbrush object that will move.")]
    public Transform toothbrushTransform;
    [Tooltip("The Start position & rotation (Right side).")]
    public Transform startPositionMarker; 
    [Tooltip("The End position & rotation (Left side).")]
    public Transform endPositionMarker;   

    [Header("Progress Settings")]
    [Tooltip("Define your bubbles and their required brush counts here.")]
    public List<BubbleThreshold> bubbleThresholds;

    [Header("Input Settings")]
    [Tooltip("Sensitivity for the rotary encoder.")]
    public float encoderSensitivity = 0.05f;
    [Tooltip("Sensitivity for keyboard input.")]
    public float keyboardSensitivity = 2.0f;
    [Tooltip("Smoothing for the brush movement.")]
    public float movementSmoothing = 10f;

    [Header("Optional Feedback")]
    public AudioSource brushingSound;

    // Internal state - Using P2 Controller (Brush)
    private ControllerInput p2_controller;
    private long lastEncoderCount;
    
    // 0.0 = End (Left), 1.0 = Start (Right)
    private float currentT = 1f; 
    private float targetT = 1f;
    private float totalBrushesCompleted = 0f;

    // Logic to handle the "Finishing Move"
    private bool isFinishing = false;

    void Start()
    {
        // 1. Initialize Controller (Player 2)
        if (HardwareManager.Instance != null)
        {
            p2_controller = HardwareManager.Instance.GetController(1); 
        }

        if (p2_controller != null)
        {
            lastEncoderCount = p2_controller.EncoderCount;
        }

        // 2. Set Initial Positions
        if (toothbrushTransform != null && startPositionMarker != null)
        {
            toothbrushTransform.position = startPositionMarker.position;
            toothbrushTransform.rotation = startPositionMarker.rotation; 
            currentT = 1f;
            targetT = 1f;
        }

        // 3. Hide all bubbles initially
        foreach(var bubble in bubbleThresholds)
        {
            if(bubble.bubbleObject != null)
                bubble.bubbleObject.SetActive(false);
        }
    }

    void Update()
    {
        if (IsTaskFinished) return;

        // If we haven't finished yet, handle input and check for win condition
        if (!isFinishing)
        {
            HandleInput();
            CheckProgress(); 
        }
        else
        {
            // We are in the "Finishing" state (moving to rest)
            CheckIfArrivedAtRest();
        }

        // Always update visuals (smooth movement)
        UpdateBrushMovement();
    }

    private void HandleInput()
    {
        float inputDelta = 0f;

        if (p2_controller != null && p2_controller.IsHardwareConnected)
        {
            long currentCount = p2_controller.EncoderCount;
            long rawDelta = currentCount - lastEncoderCount;
            lastEncoderCount = currentCount;
            inputDelta = rawDelta * encoderSensitivity;
        }
        else
        {
            // P2 Keyboard fallback
            inputDelta = Input.GetAxis("Horizontal_P2") * keyboardSensitivity * Time.deltaTime;
        }

        // Store direction for the finish snap logic
        lastInputDelta = inputDelta;

        targetT += inputDelta; 
        targetT = Mathf.Clamp01(targetT);
    }

    private void UpdateBrushMovement()
    {
        if (toothbrushTransform == null || startPositionMarker == null || endPositionMarker == null) return;

        // Smoothly interpolate current T towards target T
        float oldT = currentT;
        currentT = Mathf.Lerp(currentT, targetT, movementSmoothing * Time.deltaTime);

        // --- Position Interpolation ---
        toothbrushTransform.position = Vector3.Lerp(endPositionMarker.position, startPositionMarker.position, currentT);

        // --- Rotation Interpolation ---
        toothbrushTransform.rotation = Quaternion.Lerp(endPositionMarker.rotation, startPositionMarker.rotation, currentT);

        // Calculate Movement Amount
        float deltaMoved = Mathf.Abs(currentT - oldT);
        
        // Logic for Sound and Scoring (only if not finishing yet)
        if (!isFinishing && deltaMoved > 0.001f)
        {
            totalBrushesCompleted += deltaMoved;
            
            if (brushingSound)
            {
                if (!brushingSound.isPlaying) brushingSound.Play();
                // Pitch goes up slightly as you scrub faster
                brushingSound.pitch = Mathf.Lerp(0.8f, 1.5f, deltaMoved * 50f); 
            }
        }
        else
        {
            if (brushingSound) brushingSound.Stop();
        }
    }

    private void CheckProgress()
    {
        bool allBubblesComplete = true;

        for(int i = 0; i < bubbleThresholds.Count; i++)
        {
            BubbleThreshold step = bubbleThresholds[i];
            
            if (totalBrushesCompleted >= step.brushesNeeded)
            {
                if (step.bubbleObject != null && !step.bubbleObject.activeSelf)
                {
                    step.bubbleObject.SetActive(true);
                }
            }
            else
            {
                allBubblesComplete = false;
            }
        }

        // If all bubbles are done, start the finishing sequence
        if (allBubblesComplete && bubbleThresholds.Count > 0)
        {
            StartFinishingSequence();
        }
    }

    private void StartFinishingSequence()
    {
        isFinishing = true; 
        // --- UPDATED LOGIC HERE ---
        // Snap based on the last movement direction
        
        if (lastInputDelta > 0) 
        {
            // User was moving Right -> Snap to Start (Right/1.0)
            targetT = 1f; 
        }
        else if (lastInputDelta < 0)
        {
            // User was moving Left -> Snap to End (Left/0.0)
            targetT = 0f; 
        }
        else 
        {
            // Fallback: if input was exactly 0, go to closest side
            if (currentT > 0.5f) targetT = 1f; else targetT = 0f;
        }
    }

    private void CheckIfArrivedAtRest()
    {
        if (Mathf.Abs(currentT - targetT) < 0.02f)
        {
            currentT = targetT; 
            if (brushingSound) brushingSound.Stop();
            
            // Mark task as done so the Level Manager knows
            IsTaskFinished = true;
        }
    }
}