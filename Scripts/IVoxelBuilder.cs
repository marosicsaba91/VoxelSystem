
using System.Collections.Generic;

namespace VoxelSystem
{
	public interface IVoxelBuilder
	{
		int PaletteLength { get; }

		IEnumerable<PaletteItem> GetPaletteItems();
	}
}
