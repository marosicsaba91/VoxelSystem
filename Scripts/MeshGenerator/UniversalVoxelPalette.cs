using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "Universal Voxel Palette", menuName = "Voxel System/Universal Voxel Palette", order = 1)]
	public class UniversalVoxelPalette : ScriptableObject, IPalette
	{
		[SerializeField] List<UniversalVoxelPaletteItem> _items;
		public IReadOnlyList<UniversalVoxelPaletteItem> VoxelTypes => _items;
		public IReadOnlyList<IPaletteItem> PaletteItems => _items;
	}

	public abstract class UniversalVoxelPaletteItem : ScriptableObject, IPaletteItem
	{
		[SerializeField] string niceName;
		[SerializeField] Color color;

		public string DisplayName => niceName.IsNullOrEmpty() ? name : niceName;

		public Color DisplayColor => color;

		internal abstract void BeforeMeshGeneration(VoxelMap map, UniversalVoxelPalette palette, int voxelTypeIndex);

		internal abstract void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int voxelTypeIndex,
			List<Vector3> vertexOut,
			List<Vector3> normalOut,
			List<Vector2> uvOut,
			List<int> triangleOut);
	}
}