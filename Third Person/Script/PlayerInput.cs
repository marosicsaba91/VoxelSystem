using UnityEngine;

static class PlayerInput
{
	static readonly float onePerSqrt2 = 1 / Mathf.Sqrt(2f);

	public static Vector3 GetMovementVector() => GetVec3(Right, Left, Forward, Back);
	static Vector3 GetVec3(bool right, bool left, bool forward, bool back)
	{
		float x = !(right ^ left) ? 0 : right ? 1 : -1;
		float y = !(forward ^ back) ? 0 : forward ? 1 : -1;
		return x == 0 || y == 0 ? new Vector3(x, 0, y) : new Vector3(x * onePerSqrt2, 0, y * onePerSqrt2);
	}

	public static bool Jump => Input.GetKeyDown(KeyCode.Space);
	public static bool Right => Input.GetKey(KeyCode.D);
	public static bool Left => Input.GetKey(KeyCode.A);
	public static bool Forward => Input.GetKey(KeyCode.W);
	public static bool Back => Input.GetKey(KeyCode.S);

	public static bool RightTurn => Input.GetKeyDown(KeyCode.E);
	public static bool LeftTurn => Input.GetKeyDown(KeyCode.Q);

	public static bool Up => Input.GetKey(KeyCode.R);
	public static bool Down => Input.GetKey(KeyCode.F);
}

