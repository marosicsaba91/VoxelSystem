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
			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				if (!IsMapSideVisible(voxelEditor, side)) continue;
				Vector3 position = GetMapSidePosition(voxelEditor, side);

				string text = voxelEditor.HasSelection() ? null :
					voxelEditor.ToolState == ToolState.Drag && handleDragDirection == side
					? handleSteps.ToString() : null;

				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Arrow,
					position = position,
					direction = side,
					side = side,
					text = text
				};
			}
		}

		protected override MapChange OnHandleDown(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo)
		{
			_originalSelectionMap = voxelEditor.SeparateSelection();
			_lastOffset = Vector3Int.zero;
			return MapChange.None;
		}

		protected override MapChange OnHandleDrag(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			if (voxelEditor.HasSelection())
				return DragSelection(voxelEditor, handleInfo, steps);
			else
			{
				DragWholeMap(voxelEditor, handleInfo, steps);
				return MapChange.None;
			}
		}


		MapChange DragSelection(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			BoundsInt selection = originalSelection;
			GeneralDirection3D direction = handleInfo.direction;

			Vector3Int offset = direction.ToVectorInt() * steps;
			offset = Vector3Int.Max(offset, Vector3Int.zero - selection.min);
			offset = Vector3Int.Min(offset, originalMapSize - selection.max);

			if (offset == _lastOffset)
				return MapChange.None;

			Reset(voxelEditor);

			voxelEditor.RecordForUndo("Voxel Selection Moved", RecordType.Map | RecordType.Editor);

			VoxelMap map = voxelEditor.Map;

			map.ClearRange(selection.min, selection.max - Vector3Int.one);

			selection.position += offset;

			map.CopyFrom(_originalSelectionMap, Vector3Int.zero, selection.position, selection.size, voxelEditor.SelectedAction.ToTransformAction());
			voxelEditor.Selection = selection;
			_lastOffset = offset;

			return MapChange.Edit;
		}

		protected sealed override MapChange OnHandleUp(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps) =>
			voxelEditor.HasSelection() ? MapChange.Final : MapChange.None;

		static void DragWholeMap(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			voxelEditor.RecordForUndo("VoxelMap Moved", RecordType.Transform);
			Translate(voxelEditor, handleInfo.direction, steps);
		}
	}
}
