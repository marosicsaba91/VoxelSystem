using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Mirror : VoxelToolHandler
	{
		protected override IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{ 
			Vector3Int mapSize = voxelEditor.Map.FullSize;
			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				if (!IsMapSideVisible(voxelEditor, mapSize, side)) continue;
				 
				Vector3 position = GetMapSidePosition(mapSize, side);

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
			voxelEditor.RecordForUndo("VoxelMap Mirrored", RecordType.Map);
			voxelEditor.Map.Mirror(handleInfo.direction.GetAxis());
			return true;
		}
	}
}
