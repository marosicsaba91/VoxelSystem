using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Box : VoxelToolHandler
	{
		BoundsInt _lastBound;
		bool _lastTimeMapChanged = false;
		public sealed override VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => allVoxelActions;

		const RecordType recordType = RecordType.Map;
		protected sealed override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = voxelEditor.SelectedAction.IsAdditive();
			return true;
		}
		protected sealed override bool OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			VoxelMap map = voxelEditor.Map;
			voxelEditor.RecordForUndo("BoxTool used on VoxelMap", recordType);
			_lastBound = new (hit.voxelIndex, Vector3Int.one);

			_lastTimeMapChanged = map.SetVoxel(hit.voxelIndex, voxelEditor.SelectedAction, voxelEditor.SelectedPaletteIndex);
			return _lastTimeMapChanged;
		}

		protected override bool OnVoxelCursorDrag(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			VoxelMap map = voxelEditor.Map;

			var min = Vector3Int.Min(_mouseDownHit.voxelIndex, hit.voxelIndex);
			var max = Vector3Int.Max(_mouseDownHit.voxelIndex, hit.voxelIndex);

			BoundsInt bound = new(min, max - min + Vector3Int.one);

			map.CopyFrom(_originalMap, _lastBound.min, _lastBound.min, _lastBound.size, VoxelAction.Overwrite); 
			voxelEditor.RecordForUndo("BoxTool used on VoxelMap", recordType);

			bool mapChanged = map.SetRange(min, max, voxelEditor.SelectedAction, voxelEditor.SelectedPaletteIndex);

			bool updateMap = mapChanged || _lastTimeMapChanged;
			_lastTimeMapChanged = mapChanged;
			_lastBound = bound;

			return updateMap;
		}
	}
}
