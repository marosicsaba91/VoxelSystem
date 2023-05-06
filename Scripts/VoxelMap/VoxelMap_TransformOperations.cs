using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	partial class VoxelMap
	{
		// ------------- Transform Operations -------------
		public void ApplyRotation(Transform transform)
		{
			if (transform.localRotation == Quaternion.identity) return;

			Vector3 transformedOne = transform.TransformDirection(Vector3.one);
			Vector3 transformedSize =
				transform.TransformDirection(Vector3.right).normalized * FullSize.x +
				transform.TransformDirection(Vector3.up).normalized * FullSize.y +
				transform.TransformDirection(Vector3.forward).normalized * FullSize.z;
			Vector3 step = (Vector3.one - transformedOne) / 2f;
			Vector3 move = step.MultiplyAllAxis(transformedSize);
			transform.localPosition += move;


			int actionCount = 0; // For safety
			Vector3Int rotated;
			do
			{
				Vector3 localRotation = transform.localRotation.eulerAngles;
				rotated = new Vector3Int(
					Mathf.RoundToInt((localRotation.x % 360) / 90f),
					Mathf.RoundToInt((localRotation.y % 360) / 90f),
					Mathf.RoundToInt((localRotation.z % 360) / 90f));

				if (rotated.x != 0)
				{
					Turn(Axis3D.X, leftHandPositive: false);
					transform.Rotate(Vector3.right, angle: -90);
					actionCount++;
					continue;
				}
				if (rotated.y != 0)
				{
					Turn(Axis3D.Y, leftHandPositive: false);
					transform.Rotate(Vector3.up, angle: -90);
					actionCount++;
					continue;
				}
				if (rotated.z != 0)
				{
					Turn(Axis3D.Z, leftHandPositive: false);
					transform.Rotate(Vector3.forward, angle: -90);
					actionCount++;
				}
			}
			while (rotated != Vector3Int.zero && actionCount < 100);

		}

		public void ApplyScale(Transform transform)
		{
			if (transform.localScale == Vector3.one) return;

			Vector3 move = Vector3.zero;
			move += ApplyScaleOnAxis(transform, Axis3D.X);
			move += ApplyScaleOnAxis(transform, Axis3D.Y);
			move += ApplyScaleOnAxis(transform, Axis3D.Z);
			transform.localScale = Vector3.one;
			transform.position += transform.TransformVector(move);
		}

		Vector3 ApplyScaleOnAxis(Transform transform, Axis3D axis)
		{
			Transform trans = transform;
			Vector3 localScale = trans.localScale;
			float saleFloat =
				axis == Axis3D.X ? localScale.x :
				axis == Axis3D.Y ? localScale.y :
				localScale.z;

			int scale = Mathf.RoundToInt(saleFloat);
			Vector3Int originalSize = FullSize;
			int size =
				axis == Axis3D.X ? originalSize.x :
				axis == Axis3D.Y ? originalSize.y :
				originalSize.z;
			GeneralDirection3D positiveDir =
				axis == Axis3D.X ? GeneralDirection3D.Right :
				axis == Axis3D.Y ? GeneralDirection3D.Up :
				GeneralDirection3D.Forward;

			Vector3 move = Vector3.zero;
			if (scale < 0)
			{
				move =
				axis == Axis3D.X ? new Vector3(scale * size, y: 0, z: 0) :
				axis == Axis3D.Y ? new Vector3(x: 0, scale * size, z: 0) :
				new Vector3(x: 0, y: 0, scale * size);

				scale *= -1;
				Mirror(axis);
			}
			if (scale != 1 && scale != 0)
			{
				Resize(positiveDir, (scale - 1) * size);
			}
			return move;
		}

		public abstract void Turn(Axis3D axis, bool leftHandPositive);
		public abstract void Mirror(Axis3D axis);
		public abstract void Resize(GeneralDirection3D direction, int steps);
		public abstract void ResizeCanvas(GeneralDirection3D direction, int steps, bool repeat);
	}
}
