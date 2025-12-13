using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ControllerInput : MonoBehaviour
{
    public int ControllerID { get; private set; }
    public long EncoderCount { get; private set; }
    public bool IsButtonPressed { get; private set; }

    // check if physical hardware is actually connected.
    public bool IsHardwareConnected => serialPort != null && serialPort.IsOpen;

    private int playerIndex;
    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning = false;
    private volatile string latestData = "";

    // Sets up the controller for a specific player index and attempts to connect to the hardware
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
        bool hardwareButtonState = false;
        // If hardware is connected and has sent new data, process it.
        if (IsHardwareConnected && !string.IsNullOrEmpty(latestData))
        {
            hardwareButtonState = ParseDataAndGetButtonState(latestData);
            // Clear the data after processing
            latestData = "";
        }

        // First check hardware button state, then fallback to keyboard input if not pressed.
        if (hardwareButtonState)
        {
            IsButtonPressed = true;
        }
        else if (playerIndex == 0 && Input.GetKeyDown(KeyCode.E))
        {
            IsButtonPressed = true;
        }
        else if (playerIndex == 1 && Input.GetKeyDown(KeyCode.Return))
        {
            IsButtonPressed = true;
        }
        else
        {
            // If no input was detected this frame, the button is not pressed.
            IsButtonPressed = false;
        }
    }

    // Parses the raw string from the serial port and updates the controller's state.
    private bool ParseDataAndGetButtonState(string data)
    {
        string[] parts = data.Split(',');
        if (parts.Length == 3)
        {
            int.TryParse(parts[0], out int id);
            long.TryParse(parts[1], out long count);
            bool buttonState = (parts[2].Trim() == "1");

            ControllerID = id;
            EncoderCount = count;
            return buttonState;
        }
        return false;
    }

    // Attempts to open a connection to the specified serial port and starts the listening thread.
    void ConnectToController(string portName, int baudRate)
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 200;
            serialPort.Open();
            isRunning = true;
            readThread = new Thread(ReadData);
            readThread.Start();
            Debug.Log($"Successfully connected to controller on {portName}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not connect to {portName}. Using keyboard fallback. Error: {e.Message}");
        }
    }

    private void ReadData()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try 
            { 
                latestData = serialPort.ReadLine(); 
            }
            // A timeout exception is normal and expected if no new data arrives.
            catch (System.TimeoutException) {}
        }
    }

    void OnDestroy()
    {
        // Signal the reading thread to stop.
        isRunning = false;
        // Wait for the thread to finish its current loop before continuing.
        if (readThread != null && readThread.IsAlive) readThread.Join();
        // Close the serial port to release it.
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();
    }
}