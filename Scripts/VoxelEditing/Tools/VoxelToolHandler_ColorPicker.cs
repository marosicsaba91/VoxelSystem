using MUtility;
using System;
using System.Linq;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_ColorPicker : VoxelToolHandler
	{
		protected override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = false;
			return true;
		}

		protected override void OnDrawCursor(IVoxelEditor voxelEditor, Color actionColor, VoxelHit hit)
		{
			int paletteIndex = voxelEditor.SelectedPaletteIndex;
			paletteIndex = Mathf.Clamp(paletteIndex, 0, voxelEditor.PaletteLength - 1);
			Color color = voxelEditor.PaletteItems.ElementAt(paletteIndex).Color;
			base.OnDrawCursor(voxelEditor, color, hit);
		}

		protected override bool OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit) => Pick(voxelEditor, hit.voxelIndex);

		protected override bool OnVoxelCursorDrag(IVoxelEditor voxelEditor, VoxelHit hit) => Pick(voxelEditor, hit.voxelIndex);

		bool Pick(IVoxelEditor voxelEditor, Vector3Int inxed)
		{ 
			voxelEditor.SelectedPaletteIndex = voxelEditor.Map.GetVoxel(inxed);
			return false;
		}
	}
}
