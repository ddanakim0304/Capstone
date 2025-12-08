using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShowerGameManager : MiniGameManager
{
    // Public property for the Coordinator script to check
    public bool IsTaskFinished { get; private set; } = false;

    // To track direction for the finishing snap
    private float lastInputDelta = 0f;

    [System.Serializable]
    public class DirtThreshold
    {
        [Tooltip("The dirt object to hide.")]
        public GameObject dirtObject;
        [Tooltip("How many full shower passes (start->end) needed to clean this dirt.")]
        public float brushesNeeded;
    }

    [Header("Setup")]
    [Tooltip("The shower head object that will move.")]
    public Transform showerHeadTransform;
    [Tooltip("The Start position & rotation (e.g. Right side).")]
    public Transform startPositionMarker; 
    [Tooltip("The End position & rotation (e.g. Left side).")]
    public Transform endPositionMarker;   

    [Header("Progress Settings")]
    [Tooltip("Define your dirt patches here.")]
    public List<DirtThreshold> dirtThresholds;

    [Header("Input Settings")]
    [Tooltip("Sensitivity for the rotary encoder.")]
    public float encoderSensitivity = 0.05f;
    [Tooltip("Sensitivity for keyboard input.")]
    public float keyboardSensitivity = 2.0f;
    [Tooltip("Smoothing for the movement.")]
    public float movementSmoothing = 10f;

    [Header("Audio")]
    public AudioSource waterSound;

    // Internal state - Using P1 Controller (Shower)
    private ControllerInput p1_controller;
    private long lastEncoderCount;
    
    // 0.0 = End (Left), 1.0 = Start (Right)
    private float currentT = 1f; 
    private float targetT = 1f;
    private float totalMovementCompleted = 0f;

    // Logic to handle the "Finishing Move"
    private bool isFinishing = false;

    void Start()
    {
        // 1. Initialize Controller (Player 1)
        if (HardwareManager.Instance != null)
        {
            p1_controller = HardwareManager.Instance.GetController(0); 
        }

        if (p1_controller != null)
        {
            lastEncoderCount = p1_controller.EncoderCount;
        }

        // 2. Set Initial Positions
        if (showerHeadTransform != null && startPositionMarker != null)
        {
            showerHeadTransform.position = startPositionMarker.position;
            showerHeadTransform.rotation = startPositionMarker.rotation; 
            currentT = 1f;
            targetT = 1f;
        }

        // 3. Ensure all dirt is VISIBLE at the start
        foreach(var dirt in dirtThresholds)
        {
            if(dirt.dirtObject != null)
                dirt.dirtObject.SetActive(true);
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
        UpdateShowerMovement();
    }

    private void HandleInput()
    {
        float inputDelta = 0f;

        if (p1_controller != null && p1_controller.IsHardwareConnected)
        {
            long currentCount = p1_controller.EncoderCount;
            long rawDelta = currentCount - lastEncoderCount;
            lastEncoderCount = currentCount;
            inputDelta = rawDelta * encoderSensitivity;
        }
        else
        {
            // P1 Keyboard fallback
            inputDelta = Input.GetAxis("Horizontal_P1") * keyboardSensitivity * Time.deltaTime;
        }

        // Store direction for the finish snap logic
        lastInputDelta = inputDelta;

        targetT += inputDelta; 
        targetT = Mathf.Clamp01(targetT);
    }

    private void UpdateShowerMovement()
    {
        if (showerHeadTransform == null || startPositionMarker == null || endPositionMarker == null) return;

        // Smoothly interpolate current T towards target T
        float oldT = currentT;
        currentT = Mathf.Lerp(currentT, targetT, movementSmoothing * Time.deltaTime);

        // --- Position Interpolation ---
        showerHeadTransform.position = Vector3.Lerp(endPositionMarker.position, startPositionMarker.position, currentT);

        // --- Rotation Interpolation ---
        showerHeadTransform.rotation = Quaternion.Lerp(endPositionMarker.rotation, startPositionMarker.rotation, currentT);

        // Calculate Movement Amount
        float deltaMoved = Mathf.Abs(currentT - oldT);

        // Logic for Sound and Scoring (only if not finishing yet)
        if (!isFinishing && deltaMoved > 0.001f)
        {
            totalMovementCompleted += deltaMoved;
            
            if (waterSound)
            {
                if (!waterSound.isPlaying) waterSound.Play();
                // Randomize pitch slightly to sound like flowing water
                waterSound.pitch = Mathf.Lerp(0.9f, 1.1f, deltaMoved * 50f); 
            }
        }
        else
        {
            if (waterSound) waterSound.Stop();
        }
    }

    private void CheckProgress()
    {
        bool isAllDirtCleaned = true;

        for(int i = 0; i < dirtThresholds.Count; i++)
        {
            DirtThreshold step = dirtThresholds[i];
            
            if (totalMovementCompleted >= step.brushesNeeded)
            {
                if (step.dirtObject != null && step.dirtObject.activeSelf)
                {
                    step.dirtObject.SetActive(false);
                }
            }
            else
            {
                isAllDirtCleaned = false;
            }
        }

        // If all dirt is gone, start the finishing sequence
        if (isAllDirtCleaned && dirtThresholds.Count > 0)
        {
            StartFinishingSequence();
        }
    }

    private void StartFinishingSequence()
    {
        isFinishing = true;

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
            if (waterSound) waterSound.Stop();

            // Mark task as done so the Level Manager knows
            IsTaskFinished = true;
        }
    }
}