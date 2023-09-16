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

		// Material Palette
		int SelectedMaterialIndex { get; set; }  
		IPalette MaterialPalette { get; }

		// Shape Palette
		int SelectedShapeIndex { get; set; }  
		IPalette ShapePalette { get; }

		// VoxelValue
		int SelectedVoxelValue { get; set; }

		// Selection
		BoundsInt Selection { get; set; }
		ToolState ToolState { get; set; }
	}
}
