namespace VoxelSystem
{
	public class VoxelToolHandler_Face : VoxelToolHandler
	{
		public sealed override VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => allVoxelActions;
	}
}
