using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "Universal Voxel Palette", menuName = "Voxel System/Universal Voxel Palette", order = 1)]
	public class UniversalVoxelPalette : ScriptableObject, IVoxelPalette<UniversalVoxelPaletteItem>
	{
		[SerializeField] List<UniversalVoxelPaletteItem> _items;

		public IEnumerable<UniversalVoxelPaletteItem> Items => _items;

		public IReadOnlyList<UniversalVoxelPaletteItem> ItemsList => _items;

		public int Length => _items.Count;
	}
}