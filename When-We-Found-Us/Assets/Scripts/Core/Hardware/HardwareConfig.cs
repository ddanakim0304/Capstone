using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HardwareConfig", menuName = "Hardware/Hardware Config")]
public class HardwareConfig : ScriptableObject
{
    [System.Serializable]
    public class ControllerSetup
    {
        public string portName = "";
        public int baudRate = 115200;
    }
    
    [Header("Hardware Configuration")]
    [Tooltip("Add any real hardware controllers you want to connect to here.")]
    public List<ControllerSetup> hardwareControllers = new List<ControllerSetup>();
    
    [Tooltip("Ensures that at least this many controllers exist for keyboard fallback, even if none are connected.")]
    public int minPlayerCount = 2;
}