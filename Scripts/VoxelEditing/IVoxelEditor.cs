using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public interface IVoxelEditor
	{
		Object MapContainer { get; }
		Object EditorObject { get; }

		Transform Transform { get; }

		// Map
		VoxelMap Map { get; }
		string MapName { get; }

		// Edit
		TransformLock TransformLock { get; set; }
		VoxelTool SelectedTool { get; set; }
		VoxelAction SelectedAction { get; set; }

		// Material Palette
		int SelectedMaterialIndex { get; set; }
		List<Material> MaterialPalette { get; }

		// Shape Palette
		int SelectedShapeIndex { get; set; }  
		VoxelShapePalette ShapePalette { get; }

		// VoxelValue
		int SelectedVoxelValue { get; set; }

		// Selection
		BoundsInt Selection { get; set; }
		ToolState ToolState { get; set; }
	}
}
