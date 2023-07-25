using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelSystem
{

	[Serializable]
	class CubeVoxelPaletteSetting : VoxelPaletteSetting<CubeVoxelPalette, CubeVoxelPaletteItem> { }

	[CreateAssetMenu(menuName = "Voxel System/Cube Voxel Palette", order = 2)]
	public class CubeVoxelPalette : ScriptableObject, IVoxelPalette<CubeVoxelPaletteItem> 
	{
		[SerializeField]  CubeVoxelPaletteSetting[] paletteSettings = new CubeVoxelPaletteSetting[0];

		public IEnumerable<CubeVoxelPaletteItem> Items =>
			from CubeVoxelPaletteSetting setting in paletteSettings
			from CubeVoxelPaletteItem item in setting.Items
			select item;

		public int Length => paletteSettings.Length;

		public CubeVoxelPaletteItem GetItem(int i) => Items.ElementAt(i);

	}
}
