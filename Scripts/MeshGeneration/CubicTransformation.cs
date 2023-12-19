using MUtility;
using System;
using UnityEngine;
using VoxelSystem;

[Serializable]
public struct CubicTransformation
{
	// There is 48 possible transformations: 6 directions * 4 rotations * 2 flips

	public int upDirectionIndex;
	[Range(0, 3)] public int verticalRotation;
	public bool verticalFlip;
	public const int allCount = 48;

	public CubicTransformation(GeneralDirection3D upDirection, int verticalRotation, bool verticalFlip)
	{
		upDirectionIndex = GetUpDirectionIndex(upDirection);
		this.verticalRotation = verticalRotation % 4;
		this.verticalFlip = verticalFlip;
	}

	public CubicTransformation(int cubicTransformationIndex)
	{
		verticalFlip = cubicTransformationIndex % 2 == 1;
		cubicTransformationIndex /= 2;
		verticalRotation = cubicTransformationIndex % 4;
		cubicTransformationIndex /= 4;
		upDirectionIndex = cubicTransformationIndex;
	}


	public GeneralDirection3D UpDirection
	{
		get => GetUpDirection(upDirectionIndex);
		set => upDirectionIndex = GetUpDirectionIndex(value);
	}

	static GeneralDirection3D GetUpDirection(int upDirectionIndex) => upDirectionIndex switch
	{
		0 => GeneralDirection3D.Up,
		1 => GeneralDirection3D.Right,
		2 => GeneralDirection3D.Forward,
		3 => GeneralDirection3D.Down,
		4 => GeneralDirection3D.Left,
		5 => GeneralDirection3D.Back,
		_ => 0
	};

	static int GetUpDirectionIndex(GeneralDirection3D upDirection) => upDirection switch
	{
		GeneralDirection3D.Up => 0,
		GeneralDirection3D.Right => 1,
		GeneralDirection3D.Forward => 2,
		GeneralDirection3D.Down => 3,
		GeneralDirection3D.Left => 4,
		GeneralDirection3D.Back => 5,
		_ => 0
	};


	static Vector3 GetUpVector(int upDirectionIndex) => upDirectionIndex switch
	{
		0 => Vector3.up,        // Up
		1 => Vector3.right,     // Right
		2 => Vector3.forward,   // Forward
		3 => Vector3.down,      // Down
		4 => Vector3.left,      // Left
		5 => Vector3.back,      // Back
		_ => Vector3.up
	};

	static Vector3 GetForwardVector(int upDirectionIndex) => upDirectionIndex switch
	{
		0 => Vector3.forward,   // Up
		1 => Vector3.forward,   // Right
		2 => Vector3.down,      // Forward
		3 => Vector3.back,      // Down
		4 => Vector3.forward,   // Left
		5 => Vector3.up,        // Back
		_ => Vector3.forward
	};

	static GeneralDirection3D GetForwardDirection(int upDirectionIndex) => upDirectionIndex switch
	{
		0 => GeneralDirection3D.Forward,   // Up
		1 => GeneralDirection3D.Forward,   // Right
		2 => GeneralDirection3D.Down,      // Forward
		3 => GeneralDirection3D.Back,      // Down
		4 => GeneralDirection3D.Forward,   // Left
		5 => GeneralDirection3D.Up,        // Back
		_ => GeneralDirection3D.Forward
	};

	static GeneralDirection3D GetRightDirection(int upDirectionIndex) => upDirectionIndex switch
	{
		0 => GeneralDirection3D.Right,     // Up
		1 => GeneralDirection3D.Down,      // Right
		2 => GeneralDirection3D.Right,     // Forward
		3 => GeneralDirection3D.Right,     // Down
		4 => GeneralDirection3D.Up,        // Left
		5 => GeneralDirection3D.Right,     // Back
		_ => GeneralDirection3D.Forward
	};


	public int GetIndex() => upDirectionIndex * 8 + verticalRotation * 2 + (verticalFlip ? 1 : 0);

	static readonly Matrix4x4 rightToLeftHanded =
		Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90, 0, 0), new(-1, -1, 1));

	public Matrix4x4 GetTransformation(bool fromRightHanded = false)
	{
		Vector3 upVector = GetUpVector(upDirectionIndex);
		Vector3 forwardVector = GetForwardVector(upDirectionIndex);

		verticalRotation %= 4;
		Quaternion rotation = Quaternion.LookRotation(forwardVector, upVector);
		rotation *= Quaternion.Euler(0, verticalRotation * 90, 0);


		Vector3Int scale = verticalFlip ? new Vector3Int(1, -1, 1) : Vector3Int.one;

		Matrix4x4 result = Matrix4x4.TRS(Vector3.zero, rotation, scale);
		if (fromRightHanded)
			result *= rightToLeftHanded;
		return result;
	}

	/// <summary>
	/// Generate world direction from local direction
	/// </summary>
	/// <param name="localDir">Local Direction</param>
	/// <returns>World Direction</returns>
	internal GeneralDirection3D TransformDirection(GeneralDirection3D localDir)
	{
		if (localDir == GeneralDirection3D.Up)
			return verticalFlip ? UpDirection.Opposite() : UpDirection;
		if (localDir == GeneralDirection3D.Down)
			return verticalFlip ? UpDirection : UpDirection.Opposite();


		GeneralDirection3D localForward = GetForwardDirection(upDirectionIndex);
		GeneralDirection3D localRight = GetRightDirection(upDirectionIndex);

		if (verticalRotation == 1)
		{
			GeneralDirection3D f = localForward;
			localForward = localRight;
			localRight = f.Opposite();

		}
		else if (verticalRotation == 2)
		{
			localForward = localForward.Opposite();
			localRight = localRight.Opposite();
		}
		else if (verticalRotation == 3)
		{ 
			GeneralDirection3D f = localForward;
			localForward = localRight.Opposite();
			localRight = f;
		}

		if (localDir == GeneralDirection3D.Forward)
			return localForward;
		if (localDir == GeneralDirection3D.Back)
			return localForward.Opposite();
		if (localDir == GeneralDirection3D.Right)
			return localRight;
		if (localDir == GeneralDirection3D.Left)
			return localRight.Opposite();

		return localDir;
	}


	/// <summary>
	/// Generate local direction from world direction
	/// </summary>
	/// <param name="worldDir">Global Direction</param>
	/// <returns>Local Direction</returns>
	internal GeneralDirection3D InverseTransformDirection(GeneralDirection3D worldDir)
	{
		//Generate Inverse Transformation Function based on TransformDirection

		if (worldDir == UpDirection)
			return verticalFlip ? GeneralDirection3D.Down : GeneralDirection3D.Up;
		if (worldDir == UpDirection.Opposite())
			return verticalFlip ? GeneralDirection3D.Up : GeneralDirection3D.Down;
		
		GeneralDirection3D localForward = GetForwardDirection(upDirectionIndex);
		GeneralDirection3D localRight = GetRightDirection(upDirectionIndex);

		if (verticalRotation == 1)
		{
			GeneralDirection3D f = localForward;
			localForward = localRight;
			localRight = f.Opposite();
		}
		else if (verticalRotation == 2)
		{
			localForward = localForward.Opposite();
			localRight = localRight.Opposite();
		}
		else if (verticalRotation == 3)
		{
			GeneralDirection3D f = localForward;
			localForward = localRight.Opposite();
			localRight = f;
		}

		if (worldDir == localForward)
			return GeneralDirection3D.Forward;
		if (worldDir == localForward.Opposite())
			return GeneralDirection3D.Back;
		if (worldDir == localRight)
			return GeneralDirection3D.Right;
		if (worldDir == localRight.Opposite())
			return GeneralDirection3D.Left;

		return worldDir;
	}
}
