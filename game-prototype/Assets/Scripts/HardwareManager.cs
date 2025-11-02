using UnityEngine;
using System.Collections.Generic;

public class HardwareManager : MonoBehaviour
{
    // Defines the Inspector settings for a single hardware controller.
    [System.Serializable]
    public class ControllerSetup
    {
        public string portName = "";
        public int baudRate = 115200;
        // This is populated at runtime, so it can be hidden in the Inspector.
        [HideInInspector] public ControllerInput input; 
    }

    [Header("Hardware Configuration")]
    [Tooltip("Add any real hardware controllers you want to connect to here.")]
    public List<ControllerSetup> hardwareControllers;

    [Tooltip("Ensures that at least this many controllers exist for keyboard fallback, even if none are connected.")]
    public int minPlayerCount = 2;
    
    // A singleton instance for easy access from other scripts.
    public static HardwareManager Instance { get; private set; }

    // This list holds all active controllers
    private List<ControllerInput> allControllers = new List<ControllerInput>();

    void Awake()
    {
        // Ensure only one instance of the HardwareManager exists.
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        // Create and initialize controllers for any real hardware defined in the Inspector.
        for (int i = 0; i < hardwareControllers.Count; i++)
        {
            var setup = hardwareControllers[i];
            // Create a new ControllerInput component.
            GameObject controllerObject = new GameObject($"HardwareController_Player{i} ({setup.portName})");
            controllerObject.transform.SetParent(this.transform);
            
            setup.input = controllerObject.AddComponent<ControllerInput>();
            // Initialize with a port name to trigger a hardware connection attempt.
            setup.input.Initialize(i, setup.portName, setup.baudRate);
            allControllers.Add(setup.input);
        }
        
        // If there aren't enough hardware controllers, create virtual ones for keyboard fallback.
        while (allControllers.Count < minPlayerCount)
        {
            int playerIndex = allControllers.Count;
            GameObject controllerObject = new GameObject($"VirtualController_Player{playerIndex}");
            controllerObject.transform.SetParent(this.transform);
            
            ControllerInput virtualInput = controllerObject.AddComponent<ControllerInput>();
            // Initialize without a port name, which makes it a keyboard-only controller.
            virtualInput.Initialize(playerIndex); 
            allControllers.Add(virtualInput);
        }
    }

    // A public method for other scripts to get a specific player's controller.
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