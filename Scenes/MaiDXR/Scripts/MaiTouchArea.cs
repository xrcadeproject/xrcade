using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


public partial class MaiTouchArea : Area3D
{
	[Export]
	public MaiTouchSerial touchSerial;
	private MaiTouchSerial.TouchArea touchAarea;
	private int insideColliderCount = 0;
	public static event Action touchDidChange;
	public override void _Ready()
	{
		this.AreaEntered += OnTochEntered; 
		this.AreaExited += OnTochExited;
		touchAarea = (MaiTouchSerial.TouchArea)Enum.Parse(typeof(MaiTouchSerial.TouchArea), this.Name);
	}

	private void OnTochEntered(Area3D area)
	{
		GD.Print(this.Name + "Touch Entered" + "with " + area.Name);
		insideColliderCount += 1;
		if (touchSerial != null)
			touchSerial.ChangeTouch(touchAarea, true);
        touchDidChange?.Invoke();

		if (area.Name.ToString()[0] == 'L')
			XRHaptic.instance.SendHapticPulse(XRHaptic.Hand.Left, XRHaptic.HapticType.Medium);
		else if (area.Name.ToString()[0] == 'R')
			XRHaptic.instance.SendHapticPulse(XRHaptic.Hand.Right, XRHaptic.HapticType.Medium);
	}

	private void OnTochExited(Area3D area)
	{
		GD.Print(this.Name + "Touch Exited" + "with " + area.Name);
		insideColliderCount -= 1;
        if (insideColliderCount <= 0)
        {
			if (touchSerial != null)
            	touchSerial.ChangeTouch(touchAarea, false);
            touchDidChange?.Invoke();
			insideColliderCount = 0;
        }

		if (area.Name.ToString()[0] == 'L')
			XRHaptic.instance.SendHapticPulse(XRHaptic.Hand.Left, XRHaptic.HapticType.Light);
		else if (area.Name.ToString()[0] == 'R')
			XRHaptic.instance.SendHapticPulse(XRHaptic.Hand.Right, XRHaptic.HapticType.Light);
	}
	
}
