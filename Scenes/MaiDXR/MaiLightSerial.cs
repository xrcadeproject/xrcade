using Godot;
//using Godot.Collections;
using System.IO.Ports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using Godot.Collections;
using System.Threading.Tasks;
using System.Linq;

public partial class MaiLightSerial : Node
{
    [Export]
    public string Port = "COM51";
    [Export]
    public int BaudRate = 115200;
    [Export]
	public Node3D ButtonsRoot;
	[Export]
	public Light3D BodyLight, DisplayLight, SideLight;
    [Export]
    public int FadeResolution = 16;
    [Export]
    public float ButtonEmissionMultiplier = 1.5f;
    [Export (PropertyHint.Range, "0,1")]
    public float ButtonNormalizer = 0.2f; // 0-1

    private List<MeshInstance3D> Buttons;
    private SerialPort serial;
    private Thread thread;
    private List<byte[]> ringLightDataList = new List<byte[]>();
    private bool isrequestCMD = false;
    private byte[] header = new byte[3];
    private List<byte> dataBytes = new List<byte>();
    private List<byte[]> dataBytesList = new List<byte[]>();
    private bool isFadeFlag = false;
    private int[] fadeIndex = new int[2];
    private float fadeElapsed = 0;
    private float fadeDuration = 0;
    private Color fadeColor = new Color();
    private Color fadePrevColor = new Color();
    private enum CMD : byte
    {
        StartByte = 224,
        BootMode = 253,
        Dc = 63,
        DcUpdate = 59,
        DisableResponse = 126,
        EEPRom = 123,
        EnableResponse = 125,
        LedDirect = 130,
        LedFet = 57,
        LedGs8Bit = 49,
        LedGs8BitMulti = 50,
        LedGs8BitMultiFade = 51,
        LedGs8BitUpdate = 60,
        Timeout = 17,
    }
    
    public override async void _Ready()
    {
        Buttons = ButtonsRoot.GetChildren().OfType<MeshInstance3D>().ToList();

        if (DisplayLight != null) // Godot bug fix see github.com/godotengine/godot/issues/78253
        {
            var mask = DisplayLight.LightCullMask;
            await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
            DisplayLight.LightCullMask = mask; 
        }

        // Turn off all lights
        if (Buttons == null || DisplayLight == null || BodyLight == null || SideLight == null)
        {
            GD.Print("Some Lights not found or not assigned properly. Please check the lights.");
            return;
        }
        foreach (var button in Buttons)
        {
            var material = new StandardMaterial3D()
            {
                EmissionEnabled = true,
                Emission = new Color(0, 0, 0),
                EmissionEnergyMultiplier = ButtonEmissionMultiplier
            };
                button.Mesh.SurfaceSetMaterial(0, material);
        }
            
        
        BodyLight.LightColor = new Color(0, 0, 0);
        DisplayLight.LightColor = new Color(0, 0, 0);
        SideLight.LightColor = new Color(0, 0, 0);

        // GD.Print("Ring Lights: " + RingLights.Count);
        initialSerial(Port, BaudRate);
    }
    public override void _Process(double delta)
    {
        if (dataBytesList.Count > 0)
        {
            foreach (var data in dataBytesList)
            {
                executeCMD(data);
            }
            dataBytesList.Clear();
        }
        updateRingLightFadeProcess(delta);
    }

    public override void _ExitTree()
    {
        serial.Close();
        thread.Interrupt();
    }
    private void initialSerial(string port, int baudRate)
    {
        serial = new SerialPort(port, baudRate);
        try
        {
            GD.Print("Try start LED Serial");
            serial.Open();
        }
        catch (Exception ex)
        {
            GD.Print($"Failed to Open Serial Ports: {ex}");
        }
        GD.Print($"LED Serial on {port} Started");
        thread = new Thread(new ParameterizedThreadStart(getDataListFrom));
        thread.Start(serial);
    }

    private void getDataListFrom(object Serial)
    {
        SerialPort _serial = (SerialPort)Serial;
        byte[] header = new byte[3];

        while (true)
        {
            if (!_serial.IsOpen && _serial.BytesToRead < 1)
                continue;
                
            if ((byte)_serial.ReadByte() == (byte)CMD.StartByte)
            {
                _serial.Read(header, 0, 3);

                dataBytes = new List<byte>();
                for (int i = 0; i <= header[2]; i++) 
                    dataBytes.Add((byte)_serial.ReadByte());
                // GD.Print(Port + " Raw Data: " + BitConverter.ToString(dataBytes.ToArray()));
                dataBytesList.Add(dataBytes.ToArray());
            }
        }
    }

    private bool executeCMD(byte[] data)
    {
        // GD.Print("CMD: " + data[0]);
        if (data.Length < 1)
            return false;
        switch (data[0])
        {
            case (byte)CMD.LedFet:
                setCabLight(Color.Color8(data[1], data[2], data[3]));
                return false;
            case (byte)CMD.LedGs8Bit:
            case (byte)CMD.LedGs8BitMulti:
            case (byte)CMD.LedGs8BitMultiFade:
                ringLightDataList.Add(data);

                return false;
            case (byte)CMD.LedGs8BitUpdate:
                // isFadeFlag = false;
                onRingLightUpdateCMD();
                return true;
            default:
                return false;
        }
    }

    private void onRingLightUpdateCMD()
    {
        if (ringLightDataList.Count < 1)
            return;
        // GD.Print(Port + " Ring Light Update: " + ringLightDataList.Count);
        
        foreach (var data in ringLightDataList)
        {
            // GD.Print("Ring Light Update: " + BitConverter.ToString(data));
            switch (data[0])
            {
                case (byte)CMD.LedGs8Bit:
                    // GD.Print(Port + " Ring Light Single: " + BitConverter.ToString(data));
                    setRingLight(data[1], Color.Color8(data[2], data[3], data[4]));
                    break;
                case (byte)CMD.LedGs8BitMulti:
                    // GD.Print(Port + " Ring Light Multi: " + BitConverter.ToString(data));
                    for (int i = (int)data[1]; i < (int)data[2]; i++)
                        setRingLight(i, Color.Color8(data[4], data[5], data[6]));
                    fadePrevColor = Color.Color8(data[4], data[5], data[6]);
                    break;
                case (byte)CMD.LedGs8BitMultiFade:
                    // GD.Print(Port + " Ring Light Fade: " + BitConverter.ToString(data));
                    updateRingLightFade(data);                 
                    break;
                default:
                    break;
            }
        }
        ringLightDataList.Clear();
    }

    private void updateRingLightFade(byte[] data)
    {
        fadeElapsed = 0;
        if (data[7] == 0)
            fadeDuration = 0;
        else
            fadeDuration = 4095f / (float)data[7] * 8f / 1000f;
        if (data[2] > Buttons.Count)
            data[2] = (byte)Buttons.Count;
        fadeIndex[0] = data[1];
        fadeIndex[1] = data[2];
        fadeColor = Color.Color8(data[4], data[5], data[6]);
        isFadeFlag = true;
    }

    private void updateRingLightFadeProcess(double deltaTime)
    {
        if (!isFadeFlag)
            return;
        
        fadeElapsed += (float)deltaTime;
        float progress = fadeElapsed / fadeDuration;
        
        if (progress > 1)
        {
            isFadeFlag = false;
            progress = 1;
            fadePrevColor = fadeColor;
        }
        // GD.Print(Port + " Ring Light Fade Process " + fadeElapsed + " " + fadeDuration + " " + progress);
        // GD.Print(Port + " Ring Light Fade Progress Color Before " + RingLights[0].LightColor);
        for (int i = fadeIndex[0]; i < fadeIndex[1]; i++)
        {
            var material = (StandardMaterial3D)Buttons[i].Mesh.SurfaceGetMaterial(0);
            if (material != null)
            {
                material.Emission = dimmerColor(fadePrevColor.Lerp(fadeColor, progress));
                Buttons[i].Mesh.SurfaceSetMaterial(0, material);
            }
            // Buttons[i].LightColor = dimmerColor(fadePrevColor.Lerp(fadeColor, progress));
        }
            
        // GD.Print(Port + " Ring Light Fade Progress Color " + RingLights[0].LightColor);
    }

    private void setCabLight(Color data) { 
        if (BodyLight != null)
			BodyLight.LightColor = new Color(data.R, data.R, data.R);
		if (DisplayLight != null)
			DisplayLight.LightColor = new Color(data.G, data.G, data.G);
		if (SideLight != null)
			SideLight.LightColor = new Color(data.B, data.B, data.B);
    }
    private void setRingLight(int index, Color color) { 
        // GD.Print("Ring Light Update");
        // GD.Print(index + " " + color);
        color = dimmerColor(color);

		if (Buttons != null && Buttons.Count > index)
        {
            var material = (StandardMaterial3D)Buttons[index].Mesh.SurfaceGetMaterial(0);
            if (material != null)
            {
                material.Emission = color;
                Buttons[index].Mesh.SurfaceSetMaterial(0, material);
            }
        }
    }
    private Color dimmerColor(Color color)
    {
        float colorSum = Math.Max(0, color.R + color.G + color.B - 2.5f) / 0.5f;
        float factor = colorSum * ButtonNormalizer;
        return new Color(color.R - factor, color.G - factor, color.B - factor);
    }
}
