using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class XRHaptic : Node
{
	public static XRHaptic instance;
	private XROrigin3D origin;
	private static XRNode3D leftHandNode, rightHandNode;
	private static bool initialized = false;	

	public enum Hand
	{
		Left,
		Right
	}
	public enum HapticType
	{
		Light,
		Medium,
		Heavy
	}

	private static Dictionary<string, List<double[]>> hapticSequencePreset = new Dictionary<string, List<double[]>>()
	{
		{ "Light", new List<double[]> { // List = vibration sequences
			new double[] { 0, 0.3, 0.01 }, // { frequency, amplitude, duration }
			new double[] { 0, 0, 0.01 },
			new double[] { 1000, 1, 0.0075 },
			} 
		},
		{ "Medium", new List<double[]> {
			new double[] { 0, 0.6, 0.02 },
			new double[] { 0, 0, 0.01 },
			new double[] { 1000, 1, 0.0075 },
			}
		},
		{ "Heavy", new List<double[]> {
			new double[] { 0, 1, 0.04 },
			new double[] { 0, 0, 0.01 },
			new double[] { 1000, 1, 0.0075 },
			}
		},
	};

	public override void _Ready()
	{
		initialize();
	}

	private void initialize()
	{
		if (initialized)
			return;
		instance = this;
		origin = this.GetParent<XROrigin3D>();
		leftHandNode = origin.GetNode<XRNode3D>("LeftHand");
		rightHandNode = origin.GetNode<XRNode3D>("RightHand");
		initialized = true;
	}

	public async void SendHapticPulse(Hand hand, HapticType type)
	{
		initialize();
		XRNode3D controller = hand == Hand.Left ? leftHandNode : rightHandNode;
		string hapticName = type.ToString();
		// double lastHapticTime = 0;
		foreach (var hapticData in hapticSequencePreset[hapticName])
		{
			controller.TriggerHapticPulse("haptic", hapticData[0], hapticData[1], hapticData[2], 0);
			await ToSignal(instance.GetTree().CreateTimer(hapticData[2]), "timeout");
			// lastHapticTime += hapticData[2];
		}
	}
}
