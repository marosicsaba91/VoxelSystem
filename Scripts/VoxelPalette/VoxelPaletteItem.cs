using System;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public struct VoxelPaletteItem 
	{
		[Header("Voxel Building")]
		public VoxelBlockLibrary blockLibrary;
		public Material material;

		[Header("Menu Visuals")]
		public string name;
		public Sprite image;
		public Color color;
	}
}