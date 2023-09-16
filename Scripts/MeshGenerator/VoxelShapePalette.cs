using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "Voxel Palette", menuName = "Voxel System/Voxel Palette", order = 1)]
	public class VoxelShapePalette : ScriptableObject, IPalette
	{
		[SerializeField] List<VoxelShape> _items;
		public IReadOnlyList<VoxelShape> Shapes => _items;
		public IReadOnlyList<IPaletteItem> PaletteItems => _items;
	}
}