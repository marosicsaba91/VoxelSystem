using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Mirror : VoxelToolHandler
	{
		public override VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => GetTransformActions(voxelEditor);

		protected override IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{ 
			Vector3Int mapSize = voxelEditor.Map.FullSize;
			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				if (!IsMapSideVisible(voxelEditor, mapSize, side)) continue;
				 
				Vector3 position = GetMapSidePosition(voxelEditor, side);

				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Arrow, 
					position = position,
					direction = side
				};
			}
		}

		protected override bool OnHandleClick(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo)
		{
			if (voxelEditor.HasSelection())
			{
				voxelEditor.RecordForUndo("VoxelMap Mirrored", RecordType.Map);
				MirrorSelection(voxelEditor, handleInfo.direction.GetAxis(), voxelEditor.Selection);
			}
			else
			{
				voxelEditor.RecordForUndo("VoxelMap Mirrored", RecordType.Map);
				voxelEditor.Map.Mirror(handleInfo.direction.GetAxis());
			} 
			return true;
		}

		void MirrorSelection(IVoxelEditor voxelEditor, Axis3D axis3D, BoundsInt selection)
		{  
			VoxelMap map = voxelEditor.Map;
			ArrayVoxelMap selMap = new(selection.size);
			selMap.CopyFrom(map, selection.min, Vector3Int.zero, selection.size);

			selMap.Mirror(axis3D);
				 
			map.CopyFrom(selMap, Vector3Int.zero, selection.position, selection.size, VoxelAction.Overwrite);
		}
	}
}
