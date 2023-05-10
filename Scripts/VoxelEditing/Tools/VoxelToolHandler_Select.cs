using MUtility;
using System.Collections.Generic; 
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Select : VoxelToolHandler
	{
		bool useCursor = false;
		bool useHandle = false;

		Vector3Int _start = new();

		protected override IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{
			if (useCursor || !voxelEditor.HasSelection())
				yield break;

			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				Vector3 position = GetMapSidePosition(voxelEditor, side);

				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Box,
					position = position,
					direction = side,
					text = null
				};
				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Arrow,
					position = position + side.ToVector() * _standardSpacing,
					direction = side,
					text = null
				};
			}
		}


		protected override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = false;
			return !useHandle;
		}

		protected override bool OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			useCursor = true;
			voxelEditor.RecordForUndo("Selection Changed", RecordType.Editor);
			Vector3Int mapSize = voxelEditor.Map.FullSize;
			_start = hit.voxelIndex;
			_start = Vector3Int.Min(mapSize-Vector3Int.one, _start);
			_start = Vector3Int.Max(Vector3Int.zero, _start);
			voxelEditor.Selection = new(_start, Vector3Int.one); 
			return false;
		}

		protected override bool OnVoxelCursorDrag(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			voxelEditor.RecordForUndo("Selection Changed", RecordType.Editor);

			Vector3Int mapSize = voxelEditor.Map.FullSize;
			var min = Vector3Int.Min(_start, hit.voxelIndex);
			min = Vector3Int.Max(min, Vector3Int.zero);
			var max = Vector3Int.Max(_start, hit.voxelIndex);
			max = Vector3Int.Min(max, mapSize - Vector3Int.one);
			voxelEditor.Selection = new(min, max - min + Vector3Int.one);
			// if (voxelEditor.Selection.size == mapSize)
			// voxelEditor.Deselect();
			return false;
		}

		protected override bool OnVoxelCursorUp(IVoxelEditor voxelEditor, VoxelHit hit) =>
			useCursor = false;

		// -----------------------------------------------------------------------------------------


		protected override bool OnHandleDown(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo) 
		{
			useHandle = true;
			return false;
		}

		protected override bool OnHandleDrag(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			voxelEditor.RecordForUndo("Selection Changed", RecordType.Editor);
			GeneralDirection3D direction = handleInfo.direction;
			if(handleInfo.coneType == HandeleConeType.Box)
				voxelEditor.Selection = _originalSelection.ResizeWithLimits(direction, steps, Vector3Int.zero, _originalMapSize, Vector3Int.one);
			else
				voxelEditor.Selection = _originalSelection.MoveWithLimits(direction, steps, Vector3Int.zero, _originalMapSize);
			
			return false;
		}

		protected override bool OnHandleUp(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{ 
			useHandle = false;
			return false;
		}
	}
}
