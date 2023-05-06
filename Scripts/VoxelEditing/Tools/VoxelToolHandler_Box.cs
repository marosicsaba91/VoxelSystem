using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Box : VoxelToolHandler
	{
		public sealed override VoxelAction[] SupportedActions => allVoxelActions;
			const RecordType recordType = RecordType.Map ;
		protected sealed override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = voxelEditor.SelectedAction.IsAdditive();
			return true;
		}
		protected sealed override bool OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			VoxelMap map = voxelEditor.Map;
			voxelEditor.RecordForUndo("BoxTool used on VoxelMap", recordType);

			return  map.SetVoxel(hit.voxelIndex, voxelEditor.SelectedAction, voxelEditor.SelectedPaletteIndex);

			/*
			if (!voxelEditor.HasSelection())
				return map.SetVoxel(hit.voxelIndex, voxelEditor.SelectedAction, voxelEditor.SelectedPaletteIndex);

			else 
				return voxelEditor.Selection.Contains(hit.voxelIndex) &&
					 map.SetVoxel(hit.voxelIndex, voxelEditor.SelectedAction, voxelEditor.SelectedPaletteIndex);
			*/
		}

		protected override bool OnVoxelCursorDrag(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			VoxelMap map = voxelEditor.Map;
			map.SetupFrom(_originalMap);
			voxelEditor.RecordForUndo("BoxTool used on VoxelMap", recordType);

			var min = Vector3Int.Min(_mouseDownHit.voxelIndex, hit.voxelIndex);
			var max = Vector3Int.Max(_mouseDownHit.voxelIndex, hit.voxelIndex); 


			return map.SetRange(min, max, voxelEditor.SelectedAction, voxelEditor.SelectedPaletteIndex);
		}
	}
}
