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

		protected override MapChange OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			Pick(voxelEditor, hit.voxelIndex);
			return MapChange.None;
		}

		protected override MapChange OnVoxelCursorDrag(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			Pick(voxelEditor, hit.voxelIndex);
			return MapChange.None;
		}

		void Pick(IVoxelEditor voxelEditor, Vector3Int index)
		{ 
			voxelEditor.SelectedPaletteIndex = voxelEditor.Map.GetVoxel(index);
		}
	}
}
