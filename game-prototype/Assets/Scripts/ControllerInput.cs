// ControllerInput.cs
using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ControllerInput : MonoBehaviour
{
    // Public properties that other scripts read from.
    public int ControllerID { get; private set; }
    public long EncoderCount { get; private set; }
    public bool IsButtonPressed { get; private set; }

    // Internal state for serial communication.
    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning = false;
    private volatile string latestData = ""; // Volatile for thread safety.

    // This is called by the HardwareManager, not by Unity's Start().
    public void Initialize(string port, int rate)
    {
        ConnectToController(port, rate);
    }

    void Update()
    {
        // Process the latest data received from the reading thread on the main thread.
        if (!string.IsNullOrEmpty(latestData))
        {
            ParseData(latestData);
            latestData = ""; // Clear after processing.
        }
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
            Debug.LogError($"Could not connect to {portName}. Is it plugged in and not in use? Error: {e.Message}");
        }
    }
    
    // Runs in a separate thread to avoid freezing the game.
    private void ReadData()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                latestData = serialPort.ReadLine();
            }
            catch (System.TimeoutException) { /* This is expected when no new data arrives. */ }
        }
    }

    // Parses the "ID,count,button" string from the ESP32.
    private void ParseData(string data)
    {
        string[] parts = data.Split(',');
        if (parts.Length == 3)
        {
            int.TryParse(parts[0], out int id);
            long.TryParse(parts[1], out long count);
            bool.TryParse(parts[2].Trim() == "1" ? "true" : "false", out bool buttonState);

            ControllerID = id;
            EncoderCount = count;
            IsButtonPressed = buttonState;
        }
    }

    // Ensures the serial port and thread are closed cleanly.
    void OnDestroy()
    {
        isRunning = false;

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join();
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}