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
		List<Material> MaterialPalette { get; }

		// Shape Palette 
		VoxelShapePalette ShapePalette { get; }

		public byte SelectedMaterialIndex { get; set; } 
		public uint SelectedShapeId { get; set; } 
		// VoxelValue
		Voxel SelectedVoxelValue { get; set; }

		// Selection
		BoundsInt Selection { get; set; }
		ToolState ToolState { get; set; }
	}
}
