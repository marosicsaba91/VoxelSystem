using MUtility;
using System.Collections.Generic; 
using UnityEngine;

namespace VoxelSystem
{

	public abstract class VoxelToolHandler_SizeTools : VoxelToolHandler
	{
		Vector3Int _originalSize; 

		protected override IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{
			Vector3Int mapSize = voxelEditor.Map.FullSize;
			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				Vector3 position = GetMapSidePosition(mapSize, side);

				Axis3D axis = side.GetAxis(); 
				int oSize = _originalSize.GetAxis(axis);
				string text;
				if (_isDragged && _dragDirection == side)
				{
					int steps = _handleSteps >= 0 ? _handleSteps : Mathf.Max(_handleSteps, -oSize + 1);

					text = oSize.ToString();
					if (steps < 0)
						text += " - " + Mathf.Abs(steps).ToString();
					else if (steps > 0)
						text += " + " + steps.ToString();
				}
				else
					text = voxelEditor.Map.FullSize.GetAxis(side.GetAxis()).ToString();

				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Box,
					position = position,
					direction = side,
					text = text,
				};
			}
		}

		protected override bool OnHandleDown(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo)
		{
			_originalSize = voxelEditor.Map.FullSize;
			last = false;
			return false;
		}
		 bool last = false;

		protected override bool OnHandleDrag(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			GeneralDirection3D direction = handleInfo.direction;

			Axis3D axis = handleInfo.direction.GetAxis();
			int oSize = _originalSize.GetAxis(axis);

			if (steps <= -oSize)
			{
				if (last)
					steps = -oSize + 1;
				else
					return false;
			}

			VoxelMap map = voxelEditor.Map;
			Reset(voxelEditor);

			if (last)
				voxelEditor.RecordForUndo("VoxelMap Resized", RecordType.Map | RecordType.Transform);

			if (!direction.IsPositive())
			{
				Translate(voxelEditor, direction, steps);
				steps = -steps;
			} 

			DoResize(map, direction, steps);
			return true;
		}

		protected override bool OnHandleUp(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			last = true;
			OnHandleDrag(voxelEditor, handleInfo, steps);
			return false;
		}

		protected abstract void DoResize(VoxelMap map, GeneralDirection3D direction, int steps);
	}


	public class VoxelToolHandler_Resize : VoxelToolHandler_SizeTools
	{
		protected override void DoResize(VoxelMap map, GeneralDirection3D direction, int steps)
			 => map.Resize(direction, steps);
	}

	public class VoxelToolHandler_Repeat : VoxelToolHandler_SizeTools
	{
		protected override void DoResize(VoxelMap map, GeneralDirection3D direction, int steps)
			 => map.ResizeCanvas(direction, steps, true);
	}
	public class VoxelToolHandler_ResizeCanvas : VoxelToolHandler_SizeTools
	{
		protected override void DoResize(VoxelMap map, GeneralDirection3D direction, int steps)
			 => map.ResizeCanvas(direction, steps, false);
	}
}
