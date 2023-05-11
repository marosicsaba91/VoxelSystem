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
			Vector3Int mapSize = voxelEditor.Map.FullSize;
			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				if (!IsMapSideVisible(voxelEditor, mapSize, side)) continue;
				Vector3 position = GetMapSidePosition(voxelEditor, side);

				GeneralDirection3D dir1 = side.Next();
				 
				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Arrow,
					position = position + dir1.ToVector() * _standardSpacing,
					direction = dir1,
				};

				GeneralDirection3D dir2 = dir1.Opposite();
				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Arrow,
					position = position + dir2.ToVector() * _standardSpacing,
					direction = dir2,
				};
			}
		}

		protected override bool OnHandleClick(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo)
		{
			voxelEditor.RecordForUndo("VoxelMap Turned", RecordType.Map | RecordType.Transform);

			GeneralDirection3D dir = handleInfo.direction;
			GeneralDirection3D side = dir.Previous();

			if (!IsMapSideVisible(voxelEditor, voxelEditor.Map.FullSize, side))
				side = side.Opposite(); 

			Axis3D a1 = dir.GetAxis();
			Axis3D a2 = side.GetAxis();
			Axis3D axis = 
				a1 != Axis3D.X && a2 != Axis3D.X ? Axis3D.X:
				a1 != Axis3D.Y && a2 != Axis3D.Y ? Axis3D.Y:
				Axis3D.Z;


			if (voxelEditor.HasSelection())
			{
				voxelEditor.RecordForUndo("Voxel Selection Turned", RecordType.Map | RecordType.Editor);
				TurnSelection(voxelEditor, axis, side.IsPositive() ^ dir.IsPositive()); 
			}
			else
			{
				voxelEditor.RecordForUndo("VoxelMap Turned", RecordType.Map | RecordType.Transform);
				voxelEditor.Turn(axis, side.IsPositive() ^ dir.IsPositive());
			}

			return true;
		}
		static void TurnSelection(IVoxelEditor editor, Axis3D axis, bool leftHandPositive)
		{
			BoundsInt selection = editor.Selection;
			VoxelMap map = editor.Map;
			ArrayVoxelMap selMap = new(selection.size);
			selMap.CopyFrom(map, selection.min, Vector3Int.zero, selection.size);
			selMap.Turn(axis, leftHandPositive);
			map.ClearRange(selection);
			selection = new BoundsInt(selection.position, selMap.FullSize);
			map.CopyFrom(selMap, Vector3Int.zero, selection.position, selection.size, VoxelAction.Overwrite);
			selection.Clamp(Vector3Int.zero, map.FullSize);
			editor.Selection = selection;
		}

	}
}
