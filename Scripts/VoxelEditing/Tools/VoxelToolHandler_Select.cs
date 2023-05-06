using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Select : VoxelToolHandler
	{

		Vector3Int _start = new();

		protected override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = false;
			return true;
		}

		protected override bool OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit)
		{
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
			if(voxelEditor.Selection.size == mapSize)
				voxelEditor.Selection = new();

			return false;
		}
	}
}
