using MUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public struct BlockVoxelPaletteItem : IVoxelPaletteItem
	{ 
		public VoxelBlockLibrary blockLibrary;
		public string name;
		public Color color;

		public string Name => name;
		public Color Color => color;
	}

	[Serializable]
	class BlockVoxelPaletteSetting : VoxelPaletteSetting<BlockVoxelPalette, BlockVoxelPaletteItem> { }

	[CreateAssetMenu(menuName = "Voxel System/Block Voxel Palette", order = 2)]
	public class BlockVoxelPalette : ScriptableObject, IVoxelPalette<BlockVoxelPaletteItem> 
	{

		[SerializeField] BlockVoxelPaletteSetting[] paletteSettings = new BlockVoxelPaletteSetting[0];

		public IEnumerable<BlockVoxelPaletteItem> Items =>
			from BlockVoxelPaletteSetting setting in paletteSettings
			from BlockVoxelPaletteItem item in setting.Items
			select item;

		public int Length => paletteSettings.Length;

		public BlockVoxelPaletteItem GetItem(int i) => Items.ElementAt(i);

	}
}
