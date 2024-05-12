using Godot;
using System;
using System.Diagnostics;
using System.Threading;
public partial class InjectCaptureHook : Node
{
	[Export]
	public string processName = "sinmai.exe";
	private bool isInjecInit = false;
	Process injectionProcess = new Process();
	Godot.Collections.Array output = new Godot.Collections.Array();
	Thread thread;
	int pid;
	public override void _Ready()
	{
		
        pid = OS.CreateProcess(ProjectSettings.GlobalizePath("res://InjectCapture/InjectCapture.exe"), new string[] { processName });
        Console.WriteLine("PID: " + pid);
		

		// WaitForProcess(processName, () =>
        // {
        //     GD.Print(processName + " started, launching InjectCapture...");
        //     injectionProcess.Start();
        // });
	}

	public override void _ExitTree()
	{	
		OS.Kill(pid);
	}

	public override void _Process(double delta)
	{
		if (output.Count > 0)
		{
			GD.Print(output);
			output.Clear();
		}
		// if (!isInjecInit)
		// {
		// 	initInjectionProcess();
		// }
		// var output = injectionProcess.StandardOutput.ReadToEnd();
		// if(output != "")
		// {
		// 	GD.Print(output);
		// }
	}
	private void initInjectionProcess()
	{
		if(isInjecInit)
			return;
		GD.Print("Initializing InjectCapture process...");
		isInjecInit = false;
		// Create a new process object for the second program
        
        injectionProcess.StartInfo.FileName = ProjectSettings.GlobalizePath("res://InjectCapture/InjectCapture.exe");
        injectionProcess.StartInfo.Arguments = processName;

        // Add event handler for process exit
        injectionProcess.EnableRaisingEvents = true;
        injectionProcess.Exited += (sender, e) =>
        {
            GD.Print("InjectCapture exited.");
            // You can add code here to handle the exit event
        };
		isInjecInit = true;
	}


	static void WaitForProcess(string processName, Action onProcessFound)
    {
        // Start a new process to search for the specified process name
        ProcessStartInfo psi = new ProcessStartInfo("tasklist");
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        // Start the process
        Process proc = Process.Start(psi);

        // Read the output
        string output = proc.StandardOutput.ReadToEnd();

        // Check if the process name exists in the output
        if (output.Contains(processName))
        {
            onProcessFound?.Invoke();
        }
        else
        {
            // Wait for 1 second and check again recursively
			GD.Print("Waiting for " + processName + " to start...");
            System.Threading.Thread.Sleep(1000);
            WaitForProcess(processName, onProcessFound);
        }
    }
}
