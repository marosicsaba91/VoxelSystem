using System;
using System.Collections.Generic;
using System.Linq;

namespace VoxelSystem
{
	public enum VoxelAction
	{
		Overwrite, // Set all voxels to fix value
		Attach,    // Set all EMPTY voxels to fix Value
		Erase,     // Set all voxels to EMPTY
		Repaint,   // Set all NON-EMPTY voxels to fix Value
	}

	public enum VoxelTool { None, Box, Face, FloodFill, Move, Turn, Resize, Mirror, Repeat, ResizeCanvas, Select, ColorPicker }

	public static class VoxelEditor_EnumHelper
	{
		public static readonly VoxelAction[] _allVoxelActions = Enum.GetValues(typeof(VoxelAction)).Cast<VoxelAction>().ToArray();
		public static readonly VoxelTool[] _allVoxelTools = Enum.GetValues(typeof(VoxelTool)).Cast<VoxelTool>().ToArray();

		public static readonly VoxelAction[] _transformActions = new VoxelAction[] {  VoxelAction.Overwrite, VoxelAction.Attach };

		public static bool IsTransformTool(this VoxelTool tool) => tool is
			VoxelTool.Move or VoxelTool.Turn or VoxelTool.Mirror or
			VoxelTool.Resize or VoxelTool.Repeat or VoxelTool.ResizeCanvas;

		public static bool IsSizeTool(this VoxelTool tool) => tool is
			VoxelTool.Resize or VoxelTool.Repeat or VoxelTool.ResizeCanvas;

		public static bool IsPoseTool(this VoxelTool tool) => tool is
			VoxelTool.Move or VoxelTool.Turn or VoxelTool.Mirror;

		public static bool IsCursorTool(this VoxelTool tool) => tool is
			VoxelTool.Box or VoxelTool.Face or VoxelTool.FloodFill;

		public static bool IsAdditive(this VoxelAction action) => action is VoxelAction.Attach;

		// --------------------- Voxel Tool Handlers ---------------------

		static Dictionary<VoxelTool, VoxelToolHandler> handlers = new Dictionary<VoxelTool, VoxelToolHandler>();

		public static VoxelToolHandler GetHandler(this VoxelTool tool) 
		{
			if (tool == VoxelTool.None)
				return null;

			if (handlers.TryGetValue(tool, out VoxelToolHandler handler))
				return handler;

			Type t = (tool) switch
			{

				VoxelTool.Box => typeof(VoxelToolHandler_Box),
				VoxelTool.Face => typeof(VoxelToolHandler_Face),
				VoxelTool.Select => typeof(VoxelToolHandler_Select),
				VoxelTool.Move => typeof(VoxelToolHandler_Move),
				VoxelTool.Turn => typeof(VoxelToolHandler_Turn),
				VoxelTool.Mirror => typeof(VoxelToolHandler_Mirror),
				VoxelTool.Resize => typeof(VoxelToolHandler_Resize),
				VoxelTool.Repeat => typeof(VoxelToolHandler_Repeat),
				VoxelTool.ResizeCanvas => typeof(VoxelToolHandler_ResizeCanvas),
				VoxelTool.FloodFill => typeof(VoxelToolHandler_FloodFill),
				VoxelTool.ColorPicker => typeof(VoxelToolHandler_ColorPicker),
				_ => throw new ArgumentOutOfRangeException($"No handler for tool: {nameof(tool)}", tool, null)
			};	

			var instance = (VoxelToolHandler)Activator.CreateInstance(t);
			handlers.Add(tool, instance);
			return instance;
		}

		public static VoxelAction ToTransformAction(this VoxelAction voxelAction) => voxelAction == VoxelAction.Overwrite ? VoxelAction.Overwrite : VoxelAction.Attach;
		public static string GetTooltip(this VoxelAction voxelAction) => voxelAction switch { 
			VoxelAction.Overwrite => "Overwrite all voxels with the selected value",
			VoxelAction.Attach => "Attach just to empty space",
			VoxelAction.Erase => "Erase all non-empty voxels",
			VoxelAction.Repaint => "Repaint all non-empty voxels",
			_ => throw new ArgumentOutOfRangeException(nameof(voxelAction), voxelAction, null)
		};
	}
}
