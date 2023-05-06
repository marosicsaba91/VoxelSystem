using System.Collections.Generic;
using UnityEngine;
namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "VoxelPalette", menuName = "VoxelSystem/VoxelPalette", order = 2)]
	public class VoxelPalette : ScriptableObject
	{
		[SerializeField] PaletteItem[] paletteItems = new PaletteItem[0];

		public IEnumerable<PaletteItem> Items => paletteItems;

		public int Length => paletteItems.Length;

		public PaletteItem GetItem(int i) => paletteItems[i];

	}
}


