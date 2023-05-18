using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public interface IVoxelEditor
	{
		Object MapContainer { get; }
		Object EditorObject { get; }
		Transform transform { get; }

		// Map
		VoxelMap Map { get; }
		string MapName { get; }

		// Edit
		bool EnableEdit { get; }
		TransformLock TransformLock { get; set; }
		VoxelTool SelectedTool { get; set; }
		VoxelAction SelectedAction { get; set; }

		// Palette
		int SelectedPaletteIndex { get; set; }
		int PaletteLength { get; }
		IEnumerable<IVoxelPaletteItem> PaletteItems { get; }

		// Selection

		BoundsInt Selection { get; set; }
		ToolState ToolState { get; set; }
	}
}
