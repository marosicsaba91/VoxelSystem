using System;
using System.Linq;

namespace VoxelSystem
{
	public enum VoxelAction
	{
		Override,  // Set all voxels to fix value
		Attach,      // Set all EMPTY voxels to fix Value
		Erase,     // Set all voxels to EMPTY
		Repaint,   // Set all NON-EMPTY voxels to fix Value
		Select     // Select Voxels
	}

	public enum VoxelTool { None, Box, Face, Brush, Color, Move, Turn, Mirror, Resize, Repeat, Rescale }
	public enum VoxelCommand { Clear, Fill, Separate, CopyUp }

	public static class VoxelEditEnumHelper
	{
		public static readonly VoxelAction[] _allVoxelActions = Enum.GetValues(typeof(VoxelAction)).Cast<VoxelAction>().ToArray();
		public static readonly VoxelTool[] _allVoxelTools = Enum.GetValues(typeof(VoxelTool)).Cast<VoxelTool>().ToArray();
		public static readonly VoxelCommand[] _allVoxelCommands = Enum.GetValues(typeof(VoxelCommand)).Cast<VoxelCommand>().ToArray();

		public static bool IsTransformTool(this VoxelTool tool) => tool is    // TODO: Rename to IsTransformTool
			VoxelTool.Move or VoxelTool.Turn or VoxelTool.Mirror or
			VoxelTool.Resize or VoxelTool.Repeat or VoxelTool.Rescale;

		public static bool IsSizeTool(this VoxelTool tool) => tool is
			VoxelTool.Resize or VoxelTool.Repeat or VoxelTool.Rescale;

		public static bool IsPoseTool(this VoxelTool tool) => tool is
			VoxelTool.Move or VoxelTool.Turn or VoxelTool.Mirror;

		public static bool IsCursorTool(this VoxelTool tool) => tool is
			VoxelTool.Box or VoxelTool.Face or VoxelTool.Brush or VoxelTool.Color;

		public static bool IsAdditive(this VoxelAction action) => action is 
			VoxelAction.Attach or VoxelAction.Override;
	}
}
