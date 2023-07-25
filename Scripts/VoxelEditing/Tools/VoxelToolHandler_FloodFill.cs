using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_FloodFill : VoxelToolHandler
	{
		public sealed override VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => allVoxelActions;

		protected sealed override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = voxelEditor.SelectedAction.IsAdditive();
			return true;
		}

		static readonly HashSet<Vector3Int> chunk = new();

		protected sealed override void OnDrawCursor(IVoxelEditor voxelEditor, Color actionColor, VoxelHit hit)
		{
			Drawable side = GetDrawableVoxelSide(lastValidHit);
			Draw(side, actionColor);

			VoxelMap map = voxelEditor.Map;
			VoxelMap_Search._roundLimit = 5;
			map.SearchChunk(chunk, hit.voxelIndex, voxelEditor.SelectedAction != VoxelAction.Overwrite);
			VoxelMap_Search._roundLimit = 1000;

			Vector3 half = Vector3.one * 0.5f;
			float maxDistance = 3;
			foreach (Vector3Int index in chunk)
			{
				Vector3 center = index + half;
				float distance = Vector3.Distance(index, hit.voxelIndex);
				actionColor.a = 1 - Mathf.Clamp01(distance / maxDistance);
				Cuboid cube = new(Vector3.one * 0.1f);
				Drawable drawable = cube.ToDrawable();
				drawable.Translate(center);
				Draw(drawable, actionColor);
			}
		}

		protected sealed override MapChange OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			VoxelMap map = voxelEditor.Map;
			voxelEditor.RecordForUndo("FloodFill Voxel", RecordType.Map);

			int voxel = map.GetVoxel(hit.voxelIndex);
			if (voxel == voxelEditor.SelectedPaletteIndex)
				return MapChange.None;
			if (voxel == IntVoxelUtility.emptyValue && voxelEditor.SelectedAction == VoxelAction.Erase)
				return MapChange.None;
			map.SearchChunk(chunk, hit.voxelIndex, voxelEditor.SelectedAction != VoxelAction.Overwrite);

			bool changed = false;
			foreach (Vector3Int voxelI in chunk)
				changed |= map.SetVoxel(voxelI, voxelEditor.SelectedAction, voxelEditor.SelectedPaletteIndex);
			return changed ? MapChange.Quick : MapChange.None;
		}

	}
}
