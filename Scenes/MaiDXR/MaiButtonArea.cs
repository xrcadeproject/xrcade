using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MaiButtonArea : Area3D
{
	[Export]
	public VirtualKeyCode Key1 = VirtualKeyCode.RETURN;
	[Export]
	public VirtualKeyCode Key2 = VirtualKeyCode.NULL;
	[Export]
	public VirtualKeyCode Key3 = VirtualKeyCode.NULL;
	[Export]
	public VirtualKeyCode Key4 = VirtualKeyCode.NULL;

	public override void _Ready()
	{
		this.AreaEntered += OnTochEntered; 
		this.AreaExited += OnTochExited;
	}

	private void OnTochEntered(Area3D area)
	{
		EmulateKeyboard.PressKey(Key1);
		if (Key2 == VirtualKeyCode.NULL) return;
		EmulateKeyboard.PressKey(Key2);
		if (Key3 == VirtualKeyCode.NULL) return;
		EmulateKeyboard.PressKey(Key3);
		if (Key4 == VirtualKeyCode.NULL) return;
		EmulateKeyboard.PressKey(Key4);
	}

	private void OnTochExited(Area3D area)
	{
		EmulateKeyboard.ReleaseKey(Key1);
		if (Key2 == VirtualKeyCode.NULL) return;
		EmulateKeyboard.ReleaseKey(Key2);
		if (Key3 == VirtualKeyCode.NULL) return;
		EmulateKeyboard.ReleaseKey(Key3);
		if (Key4 == VirtualKeyCode.NULL) return;
		EmulateKeyboard.ReleaseKey(Key4);
	}

}
