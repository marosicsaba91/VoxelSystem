namespace VoxelSystem
{
	public class VoxelToolHandler_FloodFill : VoxelToolHandler
	{
		public sealed override VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => allVoxelActions;

		protected override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = false;
			return true;
		}
	}
}
