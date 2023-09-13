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
		protected sealed override MapChange OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			VoxelMap map = voxelEditor.Map;
			voxelEditor.RecordForUndo("BoxTool used on VoxelMap", recordType);
			_lastBound = new (hit.voxelIndex, Vector3Int.one);

			_lastTimeMapChanged = map.SetVoxel(hit.voxelIndex, voxelEditor.SelectedAction, voxelEditor.SelectedVoxelValue);
			return _lastTimeMapChanged ? MapChange.Quick : MapChange.None;
		}

		protected sealed override MapChange OnVoxelCursorDrag(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			VoxelMap map = voxelEditor.Map;

			Vector3Int min = Vector3Int.Min(mouseDownHit.voxelIndex, hit.voxelIndex);
			Vector3Int max = Vector3Int.Max(mouseDownHit.voxelIndex, hit.voxelIndex);

			BoundsInt bound = new(min, max - min + Vector3Int.one);

			map.CopyFrom(originalMap, _lastBound.min, _lastBound.min, _lastBound.size, VoxelAction.Overwrite); 
			voxelEditor.RecordForUndo("BoxTool used on VoxelMap", recordType);

			bool mapChanged = map.SetRange(min, max, voxelEditor.SelectedAction, voxelEditor.SelectedVoxelValue);

			bool updateMap = mapChanged || _lastTimeMapChanged;
			_lastTimeMapChanged = mapChanged;
			_lastBound = bound;

			return updateMap ? MapChange.Quick : MapChange.None;
		}

		protected sealed override MapChange OnVoxelCursorUp(IVoxelEditor voxelEditor, VoxelHit hit) => MapChange.Final;
	}
}