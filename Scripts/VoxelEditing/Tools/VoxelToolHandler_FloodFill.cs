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

		protected sealed override bool OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			return voxelEditor.FloodFill(hit.voxelIndex);  
		}
	}
}
