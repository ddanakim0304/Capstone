using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ControllerInput : MonoBehaviour
{
    public int ControllerID { get; private set; }
    public long EncoderCount { get; private set; }
    public long EncoderDelta { get; private set; }
    public bool IsButtonPressed { get; private set; }
    public bool IsHardwareConnected => serialPort != null && serialPort.IsOpen;

    private int playerIndex;
    private SerialPort serialPort;
    private Thread readThread;
    private volatile bool isRunning = false;
    private volatile string latestData = "";
    private long previousEncoderCount = 0;
    private bool _lastHardwareButtonState = false;
    private readonly object serialLock = new object();

    public void Initialize(int index, string port = "", int rate = 115200)
    {
        this.playerIndex = index;
        if (!string.IsNullOrEmpty(port))
        {
            ConnectToController(port, rate);
        }
    }

    void Update()
    {
        EncoderDelta = 0;
        
        // Process Hardware Data
        string dataToProcess = null;
        lock (serialLock)
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

        // Handle Button State (Hardware OR Keyboard)
        bool keyboardPressed = false;
        
        if (playerIndex == 0)
        {
            // Button: Space or E (held)
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E)) keyboardPressed = true;
            
            // Encoder: E (tap)
            if (Input.GetKeyDown(KeyCode.E)) EncoderDelta = 1;
        }
        else if (playerIndex == 1)
        {
            // Button: Enter or RightShift (held)
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.RightShift)) keyboardPressed = true;
            
            // Encoder: Return (tap)
            if (Input.GetKeyDown(KeyCode.Return)) EncoderDelta = 1;
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
                EncoderDelta = count - previousEncoderCount;
                previousEncoderCount = count;
                EncoderCount = count;
                
                Debug.Log($"<color=yellow>[P{playerIndex}] Parsed - ID: {id}, Encoder: {count}, Delta: {EncoderDelta}, Button: {btnState}</color>");
                
                _lastHardwareButtonState = btnState;
            }
        }
        catch { /* Ignore dirty packets */ }
    }

    void ConnectToController(string portName, int baudRate)
    {
        lock (serialLock)
        {
            if (serialPort != null && serialPort.IsOpen) return;

            try
            {
                serialPort = new SerialPort(portName, baudRate);
                serialPort.ReadTimeout = 100;
                serialPort.WriteTimeout = 100;
                serialPort.DtrEnable = false; // CRITICAL: Start with DTR disabled
                serialPort.RtsEnable = false; // CRITICAL: Start with RTS disabled
                serialPort.Open();
                
                // Small delay before enabling control signals
                Thread.Sleep(50);
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;

                isRunning = true;
                readThread = new Thread(ReadSerialLoop);
                readThread.IsBackground = true;
                readThread.Start();

                Debug.Log($"<color=cyan>[P{playerIndex}] Connected to {portName}</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[P{playerIndex}] Failed to connect to {portName}: {e.Message}");
                serialPort = null;
            }
        }
    }

    private void ReadSerialLoop()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        while (isRunning)
        {
            try
            {
                SerialPort port;
                lock (serialLock)
                {
                    port = serialPort;
                }
                
                if (port == null || !port.IsOpen)
                {
                    break;
                }

                int b = port.ReadByte();
                
                if (b == -1) continue;

                char c = (char)b;
                if (c == '\n')
                {
                    string line = sb.ToString().Trim();
                    sb.Clear();
                    if (!string.IsNullOrEmpty(line))
                    {
                        lock (serialLock)
                        {
                            latestData = line;
                        }
                    }
                }
                else if (c != '\r') // Skip carriage returns
                {
                    sb.Append(c);
                }
            }
            catch (System.TimeoutException) 
            {
                // Normal - no data available
            }
            catch (ThreadInterruptedException)
            {
                // Thread was interrupted during Close()
                break;
            }
            catch (System.Exception e)
            {
                if (isRunning) 
                {
                    Debug.LogWarning($"<color=red>[P{playerIndex}] Serial read error: {e.Message}</color>");
                }
                break;
            }
        }
        
        Debug.Log($"<color=orange>[P{playerIndex}] Read thread exiting</color>");
    }

    public void Close()
    {
        Debug.Log($"<color=orange>[P{playerIndex}] Closing serial connection...</color>");
        
        // 1. Signal the thread to stop
        isRunning = false;

        // 2. Wait for thread to exit (with timeout)
        if (readThread != null && readThread.IsAlive)
        {
            if (!readThread.Join(500))
            {
                Debug.LogWarning($"[P{playerIndex}] Thread didn't exit gracefully, interrupting...");
                try
                {
                    readThread.Interrupt();
                    readThread.Join(200);
                }
                catch { }
            }
            readThread = null;
        }

        // 3. Close the serial port properly
        lock (serialLock)
        {
            if (serialPort != null)
            {
                try
                {
                    if (serialPort.IsOpen)
                    {
                        // CRITICAL FOR BLUETOOTH: Disable control signals BEFORE closing
                        serialPort.DtrEnable = false;
                        serialPort.RtsEnable = false;
                        
                        // Small delay to let signals propagate
                        Thread.Sleep(50);
                        
                        // Discard buffers
                        serialPort.DiscardInBuffer();
                        serialPort.DiscardOutBuffer();
                        
                        // Close the port
                        serialPort.Close();
                        
                        Debug.Log($"<color=green>[P{playerIndex}] Serial port closed successfully</color>");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[P{playerIndex}] Error closing port: {e.Message}");
                }
                finally
                {
                    // CRITICAL: Dispose the SerialPort object
                    try
                    {
                        serialPort.Dispose();
                    }
                    catch { }
                    
                    serialPort = null;
                }
            }
        }
        
        // 4. Extra sleep to ensure Bluetooth releases (macOS specific)
        Thread.Sleep(100);
    }

    void OnDisable()
    {
        Close();
    }

    void OnDestroy()
    {
        Close();
    }

    void OnApplicationQuit()
    {
        Close();
    }
}