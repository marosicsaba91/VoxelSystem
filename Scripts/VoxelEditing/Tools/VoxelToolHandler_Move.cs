using MUtility; 
using System.Collections.Generic; 
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Move : VoxelToolHandler
	{
		ArrayVoxelMap _originalSelectionMap;
		Vector3Int _lastOffset = Vector3Int.zero;

		public override VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => GetTransformActions(voxelEditor);

		protected override IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{ 
			Vector3Int mapSize = voxelEditor.Map.FullSize;
			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				if (!IsMapSideVisible(voxelEditor, mapSize, side)) continue;
				Vector3 position = GetMapSidePosition(voxelEditor, side);
				
				string text = voxelEditor.HasSelection() ?null:
					_currentEventType == MouseEventType.Drag && _handleDragDirection == side 
					? _handleSteps.ToString() : null;

				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Arrow,
					position = position,
					direction = side,
					text = text
				};
			}
		}

		protected override bool OnHandleDown(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo)
		{
			_originalSelectionMap = voxelEditor.SeparateSelection();
			_lastOffset = Vector3Int.zero;
			return false;
		}

		protected override bool OnHandleDrag(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			if (voxelEditor.HasSelection())
				return DragSelection(voxelEditor, handleInfo, steps);
			else
				return DragWholeMap(voxelEditor, handleInfo, steps);
		}

		bool DragSelection(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			BoundsInt selection = _originalSelection;
			GeneralDirection3D direction = handleInfo.direction;

			Vector3Int offset = direction.ToVectorInt() * steps;
			offset = Vector3Int.Max(offset, Vector3Int.zero - selection.min);
			offset = Vector3Int.Min(offset, _originalMapSize - selection.max);
			if(offset == _lastOffset)
				return false;

			Reset(voxelEditor);

			voxelEditor.RecordForUndo("Voxel Selection Moved", RecordType.Map | RecordType.Editor);
			 
			VoxelMap map = voxelEditor.Map;

			map.ClearRange(selection.min, selection.max - Vector3Int.one);
			
			selection.position += offset;
			
			map.CopyFrom(_originalSelectionMap, Vector3Int.zero, selection.position, selection.size, voxelEditor.SelectedAction.ToTransformAction());
			voxelEditor.Selection = selection;
			_lastOffset = offset;

			return true;
		}

		static bool DragWholeMap(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			voxelEditor.RecordForUndo("VoxelMap Moved", RecordType.Transform);
			Translate(voxelEditor, handleInfo.direction, steps);
			return false;
		}
	}
}
