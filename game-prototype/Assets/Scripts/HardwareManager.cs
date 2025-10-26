// HardwareManager.cs
using UnityEngine;
using System.Collections.Generic;

public class HardwareManager : MonoBehaviour
{
    // Define how to set up one controller in the Inspector.
    [System.Serializable]
    public class ControllerSetup
    {
        public string portName = "";
        public int baudRate = 115200;
        [HideInInspector] public ControllerInput input; 
    }

    // A list of all controllers
    public List<ControllerSetup> controllersToConnect;
    public static HardwareManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        // Create and initialize a ControllerInput for each setup in the list.
        foreach (var setup in controllersToConnect)
        {
            GameObject controllerObject = new GameObject($"Controller_{setup.portName}");
            controllerObject.transform.SetParent(this.transform);
            
            setup.input = controllerObject.AddComponent<ControllerInput>();
            setup.input.Initialize(setup.portName, setup.baudRate);
        }
    }

    // Public method for other scripts to get a specific controller's data.
    public ControllerInput GetController(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < controllersToConnect.Count)
        {
            return controllersToConnect[playerIndex].input;
        }
        Debug.LogWarning($"Requested controller for player index {playerIndex} is out of bounds.");
        return null;
    }
}