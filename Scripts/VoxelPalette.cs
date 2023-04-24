using System;
using UnityEngine;
namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "VoxelPalette", menuName = "VoxelSystem/VoxelPalette", order = 2)]
	public class VoxelPalette : ScriptableObject
	{
		public Material material;
		public virtual PaletteItem[] GetPaletteItems()
		{
			return Array.Empty<PaletteItem>();
		}

	}
}


