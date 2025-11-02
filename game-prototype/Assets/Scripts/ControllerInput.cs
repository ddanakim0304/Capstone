using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ControllerInput : MonoBehaviour
{
    public int ControllerID { get; private set; }
    public long EncoderCount { get; private set; }
    public bool IsButtonPressed { get; private set; }

    // This new property is true only if the serial port is successfully opened.
    public bool IsHardwareConnected => serialPort != null && serialPort.IsOpen;

    private int playerIndex;
    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning = false;
    private volatile string latestData = "";

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
        if (IsHardwareConnected && !string.IsNullOrEmpty(latestData))
        {
            hardwareButtonState = ParseDataAndGetButtonState(latestData);
            latestData = "";
        }

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
            IsButtonPressed = false;
        }
    }

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
            try { latestData = serialPort.ReadLine(); }
            catch (System.TimeoutException) { /* Normal */ }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (readThread != null && readThread.IsAlive) readThread.Join();
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();
    }
}