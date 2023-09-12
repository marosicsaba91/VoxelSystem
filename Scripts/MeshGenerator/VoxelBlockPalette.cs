using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "Universal Voxel Palette", menuName = "Voxel System/Universal Voxel Palette", order = 1)]
	public class UniversalVoxelPalette : ScriptableObject
	{
		[SerializeField] List<UniversalVoxelPaletteItem> _items;
		public IReadOnlyList<UniversalVoxelPaletteItem> ItemsList => _items;
	}

	public abstract class UniversalVoxelPaletteItem : ScriptableObject
	{
		[SerializeField] string niceName;
		[SerializeField] Color color;

		public string Name => niceName.IsNullOrEmpty() ? name : niceName;

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