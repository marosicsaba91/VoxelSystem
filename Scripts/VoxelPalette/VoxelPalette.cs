using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(menuName = "Voxel System/Voxel Palette", order = 2)]
	public class VoxelPalette : ScriptableObject, IVoxelPalette
	{
		[SerializeField] VoxelPaletteSetting[] paletteSettings = new VoxelPaletteSetting[0];

		public IEnumerable<VoxelPaletteItem> Items =>
			from VoxelPaletteSetting setting in paletteSettings
			from VoxelPaletteItem item in setting.Items
			select item;

		public int Length => paletteSettings.Length;

		public VoxelPaletteItem GetItem(int i) => Items.ElementAt(i);

	}
}