using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Turn : VoxelToolHandler
	{
		public override VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => GetTransformActions(voxelEditor);

		protected override IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{
			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				if (!IsMapSideVisible(voxelEditor, side)) continue;
				Vector3 position = GetMapSidePosition(voxelEditor, side);

				GeneralDirection3D dir1 = side.GetPerpendicularNext();
				 
				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Arrow,
					position = position + dir1.ToVector() * standardSpacing,
					direction = dir1,
					side = side,
				};

				GeneralDirection3D dir2 = dir1.Opposite();
				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Arrow,
					position = position + dir2.ToVector() * standardSpacing,
					direction = dir2,
					side = side,
				};
			}
		}

		protected override MapChange OnHandleClick(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo)
		{
			voxelEditor.RecordForUndo("VoxelMap Turned", RecordType.Map | RecordType.Transform);

			GeneralDirection3D dir = handleInfo.direction;
			GeneralDirection3D side = handleInfo.side; 
			if (!IsMapSideVisible(voxelEditor, side))
				side = side.Opposite(); 

			Axis3D a1 = dir.GetAxis();
			Axis3D a2 = side.GetAxis();
			Axis3D axis = 
				a1 != Axis3D.X && a2 != Axis3D.X ? Axis3D.X:
				a1 != Axis3D.Y && a2 != Axis3D.Y ? Axis3D.Y:
				Axis3D.Z;

			bool leftHandedPositive = dir.IsPositive() ^ !side.IsPositive();
			// Debug.Log($"Side: {side}          Dir: {dir}          Left Hand Positive: " + leftHandedPositive);
			if (voxelEditor.HasSelection())
			{
				voxelEditor.RecordForUndo("Voxel Selection Turned", RecordType.Map | RecordType.Editor); 
				TurnSelection(voxelEditor, axis, leftHandedPositive); 
			}
			else
			{
				voxelEditor.RecordForUndo("VoxelMap Turned", RecordType.Map | RecordType.Transform);
				voxelEditor.Turn(axis, leftHandedPositive);
			}

			return MapChange.Final;
		}
		static void TurnSelection(IVoxelEditor editor, Axis3D axis, bool leftHandedPositive)
		{
			BoundsInt selection = editor.Selection;
			VoxelMap map = editor.Map;
			ArrayVoxelMap selMap = new(selection.size);
			selMap.CopyFrom(map, selection.min, Vector3Int.zero, selection.size);
			selMap.Turn(axis,leftHandedPositive);
			map.ClearRange(selection);
			selection = new BoundsInt(selection.position, selMap.FullSize);
			map.CopyFrom(selMap, Vector3Int.zero, selection.position, selection.size, VoxelAction.Overwrite);
			selection.Clamp(Vector3Int.zero, map.FullSize);
			editor.Selection = selection;
		}

	}
}
