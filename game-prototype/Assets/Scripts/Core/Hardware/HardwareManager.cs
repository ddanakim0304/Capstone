using UnityEngine;
using System.Collections.Generic;

public class HardwareManager : MonoBehaviour
{
    public static HardwareManager Instance { get; private set; }
    private List<ControllerInput> allControllers = new List<ControllerInput>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        
        InitializeControllers();
    }
    
    private void InitializeControllers()
    {
        // Clear any existing controllers
        foreach (var controller in allControllers)
        {
            if (controller != null) controller.Close();
        }
        allControllers.Clear();
        
        // Auto-load config from Resources folder
        HardwareConfig config = Resources.Load<HardwareConfig>("HardwareConfig");
        
        if (config == null)
        {
            Debug.LogWarning("HardwareConfig not found in Resources folder. Creating keyboard-only controllers.");
        }
        
        var controllers = config != null ? config.hardwareControllers : new List<HardwareConfig.ControllerSetup>();
        int minPlayers = config != null ? config.minPlayerCount : 2;
        
        // Create hardware controllers
        for (int i = 0; i < controllers.Count; i++)
        {
            var setup = controllers[i];
            GameObject controllerObject = new GameObject($"HardwareController_Player{i} ({setup.portName})");
            controllerObject.transform.SetParent(this.transform);
            
            var input = controllerObject.AddComponent<ControllerInput>();
            input.Initialize(i, setup.portName, setup.baudRate);
            allControllers.Add(input);
        }
        
        // Fill with virtual (keyboard) controllers to reach minimum player count
        while (allControllers.Count < minPlayers)
        {
            int playerIndex = allControllers.Count;
            GameObject controllerObject = new GameObject($"KeyboardController_Player{playerIndex}");
            controllerObject.transform.SetParent(this.transform);
            
            var virtualInput = controllerObject.AddComponent<ControllerInput>();
            virtualInput.Initialize(playerIndex); // No port name = keyboard only
            allControllers.Add(virtualInput);
        }
        
        Debug.Log($"HardwareManager initialized with {allControllers.Count} controllers ({controllers.Count} hardware, {allControllers.Count - controllers.Count} keyboard)");
    }
    
    public ControllerInput GetController(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < allControllers.Count)
            return allControllers[playerIndex];
        
        Debug.LogWarning($"Requested controller for player index {playerIndex} is out of bounds.");
        return null;
    }
    
    public int GetControllerCount()
    {
        return allControllers.Count;
    }
    
    void OnApplicationQuit()
    {
        foreach (var controller in allControllers)
        {
            if (controller != null) controller.Close();
        }
    }
}