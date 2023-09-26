using System.Collections.Generic;
using UnityEngine;

public interface IPalette
{
	IReadOnlyList<IPaletteItem> PaletteItems { get; }
	int Count => PaletteItems.Count;
}

public interface IPaletteItem
{
	string DisplayName { get; }
	Color DisplayColor { get; }
	// Texture2D DisplayTexture { get; }

}