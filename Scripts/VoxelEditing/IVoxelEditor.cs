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
		VoxelPalette VoxelPalette { get; }

		// Selection

		BoundsInt Selection { get; set; }
	}
}
