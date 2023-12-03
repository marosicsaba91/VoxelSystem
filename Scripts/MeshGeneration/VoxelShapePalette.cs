using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "VoxelShapePalette", menuName = EditorConstants.categoryPath + "Voxel Shape Palette", order = EditorConstants.soOrder_Palette)]
	public class VoxelShapePalette : ScriptableObject
	{
		[SerializeField] List<VoxelShapeBuilder> _items;
		public IReadOnlyList<VoxelShapeBuilder> Shapes => _items;
	}
}