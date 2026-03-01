using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HardwareConfig", menuName = "Hardware/Hardware Config")]
public class HardwareConfig : ScriptableObject
{
    [System.Serializable]
    public class ControllerSetup
    {
        [Tooltip("UDP port Unity listens on for this controller (e.g. 5000 for P1, 5001 for P2). Must match UDP_PORT in the ESP32 sketch.")]
        public int udpListenPort = 5000;

        [Tooltip("IP address of the ESP32 (shown in Serial Monitor after WiFi connects). Only needed if Unity sends commands back to this device.")]
        public string espIp = "";

        [Tooltip("UDP port the ESP32 listens on for commands from Unity (CMD_RECV_PORT in sketch, default 5002).")]
        public int cmdPort = 5002;
    }
    
    [Header("Hardware Configuration")]
    [Tooltip("Add any real hardware controllers you want to connect to here.")]
    public List<ControllerSetup> hardwareControllers = new List<ControllerSetup>();
    
    [Tooltip("Ensures that at least this many controllers exist for keyboard fallback, even if none are connected.")]
    public int minPlayerCount = 2;
}