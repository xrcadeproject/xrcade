using Godot;
using System.IO.Ports;
using System;
using System.Collections;
using System.Threading;
public partial class MaiTouchSerial : Node
{
    [Export]
    public string PortName = "COM5";
    [Export]
    public int BaudRate = 9600;
    private SerialPort serial;
    private byte[] settingPacket = new byte[6] {40, 0, 0, 0, 0, 41};
    private byte[] touchData = new byte[9] {40, 0, 0, 0, 0, 0, 0, 0, 41};
    public static bool startUp = false; //use ture for default start up state to prevent restart game
    private bool isSerialReady = false;
    static string recivData;
    private Thread touchThread;
    private Queue touchQueue;
    
    public override void _Ready()
    {
        try
        {
            GD.Print($"{PortName}: Try start Touch Serial with {BaudRate} baud rate");
            serial = new SerialPort (PortName, BaudRate);
            serial.Open();
            isSerialReady = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{PortName}: Failed to Open Serial Ports: {ex}");
            isSerialReady = false;
        }
        touchQueue = Queue.Synchronized(new Queue());
        touchThread = new Thread(TouchThread);
        MaiTouchArea.touchDidChange += PingTouchThread;
        touchThread.Start();
        GD.Print($"{PortName}: Serial Started");
    }

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.T))
        {
            GD.Print("force touch");
            startUp = true;
        }
    }
	private void OnPingTouchThreadTimerTimeout()
	{
		PingTouchThread();
	}
    private void PingTouchThread()
    {
        touchQueue.Enqueue(1);
    }
    private void TouchThread()
    {
        while(true)
        {
            if(serial.IsOpen)
                ReadData(serial);
            if(touchQueue.Count > 0)
            {
                touchQueue.Dequeue();
                UpdateTouch();
            }
        }
    }
    public override void _ExitTree()
    {
        touchThread.Interrupt();
        serial.Close();
    }

    private void ReadData(SerialPort Serial)
    {
        if (Serial.BytesToRead == 6)
        {
            recivData = Serial.ReadExisting();
            TouchSetUp(Serial, recivData); 
        }
    }
    private void TouchSetUp(SerialPort Serial, string data)
    {
        switch (Convert.ToByte(data[3]))
        {
            case 76:
            case 69:
                startUp = false;
                break;
            case 114:
            case 107:
                for (int i=1; i<5; i++)
                    settingPacket[i] = Convert.ToByte(data[i]);    
                Serial.Write(settingPacket, 0, settingPacket.Length);
                break;
            case 65:
                startUp = true;
                break;
        }
    }
    private void UpdateTouch()
    {
        if (!startUp || !isSerialReady)
            return;
		serial.Write(touchData, 0, 9);
    }

    public void ChangeTouch(TouchArea touchArea, bool State)
    {
        ByteArrayExt.SetBit(touchData, (int)touchArea + 8, State);
    }
	public enum TouchArea
    {
        A1 = 0, A2 = 1, A3 = 2, A4 = 3, A5 = 4, 
        A6 = 8, A7 = 9, A8 = 10, B1 = 11, B2 = 12, 
        B3 = 16, B4 = 17, B5 = 18, B6 = 19, B7 = 20, 
        B8 = 24, C1 = 25, C2 = 26, D1 = 27, D2 = 28, 
        D3 = 32, D4 = 33, D5 = 34, D6 = 35, D7 = 36, 
        D8 = 40, E1 = 41, E2 = 42, E3 = 43, E4 = 44, 
        E5 = 48, E6 = 49, E7 = 50, E8 = 51,
    }
}

public static class ByteArrayExt
{
    public static byte[] SetBit(this byte[] self, int index, bool value)
    { 
        var bitArray = new BitArray(self);
        bitArray.Set(index, value);
        bitArray.CopyTo(self, 0);
        return self;
    }
}