using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_VoxelShapePicker : VoxelToolHandler
	{
		protected override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = false;
			return true;
		}

		protected override void OnDrawCursor(IVoxelEditor voxelEditor, Color actionColor, VoxelHit hit)
		{
			int paletteIndex = voxelEditor.SelectedShapeIndex;
			paletteIndex = Mathf.Clamp(paletteIndex, 0, voxelEditor.MaterialPalette.Count - 1);
			Color color = voxelEditor.MaterialPalette.PaletteItems[paletteIndex].DisplayColor;
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
			int selected = voxelEditor.SelectedVoxelValue;
			int picked = voxelEditor.Map.GetVoxel(index);

			selected.SetShapeIndex(picked.GetShapeIndex());
			selected.SetExtraVoxelData(picked.GetExtraVoxelData());

			voxelEditor.SelectedVoxelValue = selected;
		}
	}
}
