using MUtility;
using System;
using UnityEngine;

namespace VoxelSystem
{
	partial class OctVoxelMap
	{
		public sealed override void Turn(Axis3D axis, bool leftHandPositive)
		{
			Vector3Int size = canvasSize;
			int newW =
				axis == Axis3D.X ? size.x :
				axis == Axis3D.Y ? size.z :
				axis == Axis3D.Z ? size.y : 0;
			int newH =
				axis == Axis3D.X ? size.z :
				axis == Axis3D.Y ? size.y :
				axis == Axis3D.Z ? size.x : 0;
			int newD =
				axis == Axis3D.X ? size.y :
				axis == Axis3D.Y ? size.x :
				axis == Axis3D.Z ? size.z : 0;

			// TODO: The turning

			canvasSize = new Vector3Int(newW, newH, newD);
		}

		public sealed override void Mirror(Axis3D axis)
		{
			// TODO: The mirroring
		}

		public Vector3 ApplyScaleOnAxis(Axis3D axis, float scale)
		{
			int scaleInt = Mathf.RoundToInt(scale);
			int size =
				axis == Axis3D.X ? Width :
				axis == Axis3D.Y ? Height :
				Depth;
			GeneralDirection3D positiveDir =
				axis == Axis3D.X ? GeneralDirection3D.Right :
				axis == Axis3D.Y ? GeneralDirection3D.Up :
				GeneralDirection3D.Forward;

			Vector3 move = Vector3.zero;
			if (scaleInt < 0)
			{
				move =
				axis == Axis3D.X ? new Vector3(scaleInt * size, y: 0, z: 0) :
				axis == Axis3D.Y ? new Vector3(x: 0, scaleInt * size, z: 0) :
				new Vector3(x: 0, y: 0, scaleInt * size);

				scaleInt *= -1;
				Mirror(axis);
			}
			if (scaleInt is not 0 and not 1)
			{
				Resize(positiveDir, (scaleInt - 1) * size);
			}
			return move;
		}

		public sealed override void Resize(GeneralDirection3D direction, int steps) => throw new NotImplementedException();

		public override void ResizeCanvas(GeneralDirection3D direction, int steps, bool repeat) => throw new NotImplementedException();

		/*
		public sealed override void MirrorSelection(Axis3D axis, BoundsInt selection) => throw new NotImplementedException();

		public sealed override void TurnSelection(Axis3D axis, bool leftHandPositive, BoundsInt selection) => throw new NotImplementedException();

		public sealed override void ResizeSelection(GeneralDirection3D direction, int steps, BoundsInt selection) => throw new NotImplementedException();

		public sealed override void RepeatSelection(GeneralDirection3D direction, int steps, BoundsInt selection) => throw new NotImplementedException();

		public sealed override void MoveSelection(GeneralDirection3D direction, int steps, BoundsInt selection, VoxelAction voxelAction = VoxelAction.Attach) => throw new NotImplementedException();
		*/
	}
}