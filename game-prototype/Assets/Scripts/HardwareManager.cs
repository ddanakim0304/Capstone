using UnityEngine;
using System.Collections.Generic;

public class HardwareManager : MonoBehaviour
{
    // A class to define how to set up one hardware controller in the Inspector.
    [System.Serializable]
    public class ControllerSetup
    {
        public string portName = "";
        public int baudRate = 115200;
        // This is populated at runtime, so it doesn't need to be visible.
        [HideInInspector] public ControllerInput input; 
    }

    [Header("Hardware Configuration")]
    [Tooltip("Add any real hardware controllers you want to connect to here.")]
    public List<ControllerSetup> hardwareControllers;

    [Tooltip("Ensures that at least this many controllers exist for keyboard fallback, even if none are connected.")]
    public int minPlayerCount = 2;
    
    // Singleton instance for easy access from other scripts.
    public static HardwareManager Instance { get; private set; }

    // This private list will hold all controllers, both real and virtual.
    private List<ControllerInput> allControllers = new List<ControllerInput>();

    void Awake()
    {
        // Standard Singleton pattern to ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        // 1. Create controllers for any real hardware specified in the Inspector.
        for (int i = 0; i < hardwareControllers.Count; i++)
        {
            var setup = hardwareControllers[i];
            GameObject controllerObject = new GameObject($"HardwareController_Player{i} ({setup.portName})");
            controllerObject.transform.SetParent(this.transform);
            
            setup.input = controllerObject.AddComponent<ControllerInput>();
            setup.input.Initialize(i, setup.portName, setup.baudRate);
            allControllers.Add(setup.input);
        }
        
        // 2. Create "virtual" controllers for keyboard fallback if we don't have enough.
        while (allControllers.Count < minPlayerCount)
        {
            int playerIndex = allControllers.Count;
            GameObject controllerObject = new GameObject($"VirtualController_Player{playerIndex}");
            controllerObject.transform.SetParent(this.transform);
            
            ControllerInput virtualInput = controllerObject.AddComponent<ControllerInput>();
            virtualInput.Initialize(playerIndex); // Initialize without a port name
            allControllers.Add(virtualInput);
        }
    }

    // Public method for any script to get a player's controller.
    public ControllerInput GetController(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < allControllers.Count)
        {
            return allControllers[playerIndex];
        }
        
        Debug.LogWarning($"Requested controller for player index {playerIndex} is out of bounds.");
        return null;
    }
}