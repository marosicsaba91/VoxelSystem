using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public interface IVoxelPalette<TItem> 
	{
		public abstract IEnumerable<TItem> Items { get; }

		public abstract int Length { get; }	
	}

	public interface IVoxelPaletteItem
	{
		public string Name { get; }
		public Color Color { get; }

	}
}
