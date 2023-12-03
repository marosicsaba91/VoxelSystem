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

	public static GeneralDirection3D GetUpDirection(int upDirectionIndex) => upDirectionIndex switch
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


	Vector3 GetForwardVector() => upDirectionIndex switch
	{
		0 => Vector3.up,        // Up
		1 => Vector3.right,     // Right
		2 => Vector3.forward,   // Forward
		3 => Vector3.down,      // Down
		4 => Vector3.left,      // Left
		5 => Vector3.back,      // Back
		_ => Vector3.up
	};

	Vector3 GetForwardDirection() => upDirectionIndex switch
	{
		0 => Vector3.forward,   // Up
		1 => Vector3.forward,   // Right
		2 => Vector3.down,      // Forward
		3 => Vector3.back,      // Down
		4 => Vector3.forward,   // Left
		5 => Vector3.up,        // Back
		_ => Vector3.forward
	};


	public int GetIndex() => upDirectionIndex * 8 + verticalRotation * 2 + (verticalFlip ? 1 : 0);

	public Matrix4x4 GetTransformation()
	{
		Vector3 upVector = GetForwardVector();
		Vector3 forwardVector = GetForwardDirection();

		verticalRotation %= 4;
		Quaternion rotation = Quaternion.LookRotation(forwardVector, upVector);
		rotation *= Quaternion.Euler(0, verticalRotation * 90, 0);


		Vector3Int scale = verticalFlip ? new Vector3Int(1, -1, 1) : Vector3Int.one;
		return Matrix4x4.TRS(Vector3.zero, rotation, scale);
	}

	internal GeneralDirection3D TransformDirection(GeneralDirection3D dir)   // NEED OPTIMISATION
	{
		Vector3 vec = dir.ToVector();
		Matrix4x4 transformation = GetTransformation();
		vec = transformation.MultiplyVector(vec);
		return DirectionUtility.GeneralDirection3DFromVector(vec);
	}
}
