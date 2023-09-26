using MUtility;
using System;

[Serializable]
struct SideFlags
{
	public bool right;
	public bool left;
	public bool up;
	public bool down;
	public bool forward;
	public bool back;

	public bool this[GeneralDirection3D index] => index switch
	{
		GeneralDirection3D.Right  => right,
		GeneralDirection3D.Left  => left,
		GeneralDirection3D.Up  => up,
		GeneralDirection3D.Down  => down,
		GeneralDirection3D.Forward  => forward,
		GeneralDirection3D.Back  => back,
		_ => throw new IndexOutOfRangeException()
	};

	public SideFlags(bool value)
	{
		right = value;
		left = value;
		up = value;
		down = value;
		forward = value;
		back = value;
	}

	public SideFlags(bool right, bool left, bool up, bool down, bool forward, bool back)
	{
		this.right = right;
		this.left = left;
		this.up = up;
		this.down = down;
		this.forward = forward;
		this.back = back;
	}
}