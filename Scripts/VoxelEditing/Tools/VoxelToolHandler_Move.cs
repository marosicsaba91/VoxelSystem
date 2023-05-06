using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Move : VoxelToolHandler
	{ 

		protected override IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{ 
			Vector3Int mapSize = voxelEditor.Map.FullSize;
			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				if (!IsMapSideVisible(voxelEditor, mapSize, side)) continue;
				Vector3 position = GetMapSidePosition(mapSize, side);

				string text = _isDragged && _dragDirection == side ? _handleSteps.ToString() : null;

				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Arrow,
					position = position,
					direction = side,
					text = text
				};
			}
		}

		protected override bool OnHandleDrag(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			voxelEditor.RecordForUndo("VoxelMap Moved", RecordType.Transform);
			Translate(voxelEditor, handleInfo.direction, steps);
			return false;
		} 
	}
}
