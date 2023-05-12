using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public interface IVoxelPalette
	{

		public abstract IEnumerable<VoxelPaletteItem> Items { get; }

		public abstract int Length { get; }	


	}
}
