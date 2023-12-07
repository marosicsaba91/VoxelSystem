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
			//int paletteIndex = voxelEditor.SelectedShapeId;
			//paletteIndex = Mathf.ClampSame(paletteIndex, 0, voxelEditor.MaterialPalette.Count - 1);
			//Color color = voxelEditor.MaterialPalette.PaletteItems[paletteIndex].DisplayColor;
			base.OnDrawCursor(voxelEditor, Color.white, hit);
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
			Voxel selected = voxelEditor.SelectedVoxelValue;
			Voxel picked = voxelEditor.Map.GetVoxel(index);

			selected.shapeId = picked.shapeId;
			selected.extraVoxelData = picked.extraVoxelData;

			voxelEditor.SelectedVoxelValue = selected;
		}
	}
}
