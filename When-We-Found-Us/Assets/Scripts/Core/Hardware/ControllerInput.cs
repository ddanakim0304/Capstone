using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ControllerInput : MonoBehaviour
{
    public int ControllerID { get; private set; }
    public long EncoderCount { get; private set; }
    public long EncoderDelta { get; private set; }
    public bool IsButtonPressed { get; private set; }
    public bool IsHardwareConnected => _udpClient != null;

    private int playerIndex;
    private string _espIp;
    private int _cmdPort;
    private UdpClient _udpClient;
    private Thread readThread;
    private volatile bool isRunning = false;
    private volatile string latestData = "";
    private long previousEncoderCount = 0;
    private bool _lastHardwareButtonState = false;
    private readonly object dataLock = new object();

    public void Initialize(int index, int udpListenPort = 0, string espIp = "", int cmdPort = 0)
    {
        this.playerIndex = index;
        this._espIp = espIp;
        this._cmdPort = cmdPort;
        if (udpListenPort > 0)
        {
            StartUdpListener(udpListenPort);
        }
    }

    void Update()
    {
        EncoderDelta = 0;
        
        // Process Hardware Data
        string dataToProcess = null;
        lock (dataLock)
        {
            if (!string.IsNullOrEmpty(latestData))
            {
                dataToProcess = latestData;
                latestData = "";
            }
        }
        
        if (dataToProcess != null)
        {
            ParseData(dataToProcess);
        }

        // Keyboard button fallback — always active so keyboard can be used alongside hardware for debugging.
        // Encoder/knob keyboard fallback is intentionally absent here; each
        // mini-game implements its own encoder keyboard logic in its own script.
        bool keyboardPressed = false;
        if (playerIndex == 0)
        {
            if (Input.GetKey(KeyCode.W)) keyboardPressed = true;
        }
        else if (playerIndex == 1)
        {
            if (Input.GetKey(KeyCode.UpArrow)) keyboardPressed = true;
        }

        IsButtonPressed = _lastHardwareButtonState || keyboardPressed;
    }

    private void ParseData(string data)
    {
        try
        {
            string[] parts = data.Split(',');
            if (parts.Length == 3)
            {
                int.TryParse(parts[0], out int id);
                long.TryParse(parts[1], out long count);
                bool btnState = (parts[2].Trim() == "1");

                ControllerID = id;
                EncoderDelta = previousEncoderCount - count;
                previousEncoderCount = count;
                EncoderCount = count;
                
                Debug.Log($"<color=yellow>[P{playerIndex}] Parsed - ID: {id}, Encoder: {count}, Delta: {EncoderDelta}, Button: {btnState}</color>");
                
                _lastHardwareButtonState = btnState;
            }
        }
        catch { /* Ignore dirty packets */ }
    }

    private void StartUdpListener(int port)
    {
        try
        {
            _udpClient = new UdpClient();
            // Allow immediate rebind on the same port after a Play session stops
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            _udpClient.Client.ReceiveTimeout = 200;
            isRunning = true;
            readThread = new Thread(ReadUdpLoop);
            readThread.IsBackground = true;
            readThread.Start();
            Debug.Log($"<color=cyan>[P{playerIndex}] Listening for UDP on port {port}</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[P{playerIndex}] Failed to open UDP port {port}: {e.Message}");
            _udpClient = null;
        }
    }

    private void ReadUdpLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteEP);
                string line = System.Text.Encoding.ASCII.GetString(data).Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    lock (dataLock)
                    {
                        latestData = line;
                    }
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                // Normal timeout — no data this tick
            }
            catch (System.Exception e)
            {
                if (isRunning)
                    Debug.LogWarning($"<color=red>[P{playerIndex}] UDP read error: {e.Message}</color>");
                break;
            }
        }
    }


    /// Sends a UDP command string to the ESP32 at the configured IP and command port.
    public void SendCommand(string command)
    {
        if (string.IsNullOrEmpty(_espIp) || _cmdPort <= 0)
        {
            Debug.LogWarning($"[P{playerIndex}] Cannot send command '{command}': espIp/cmdPort not set in HardwareConfig.");
            return;
        }
        try
        {
            using (var sender = new UdpClient())
            {
                byte[] payload = System.Text.Encoding.ASCII.GetBytes(command);
                sender.Send(payload, payload.Length, new IPEndPoint(IPAddress.Parse(_espIp), _cmdPort));
                Debug.Log($"<color=green>[P{playerIndex}] Sent command '{command}' to {_espIp}:{_cmdPort}</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[P{playerIndex}] Failed to send command '{command}': {e.Message}");
        }
    }

    public void Close()
    {
        isRunning = false;

        // Closing the socket unblocks Receive() immediately
        try { _udpClient?.Close(); } catch { }

        if (readThread != null && readThread.IsAlive)
        {
            if (!readThread.Join(500))
            {
                readThread.Interrupt();
                readThread.Join(200);
            }
            readThread = null;
        }

        _udpClient = null;
    }

    void OnDisable()       { Close(); }
    void OnDestroy()       { Close(); }
    void OnApplicationQuit() { Close(); }
}