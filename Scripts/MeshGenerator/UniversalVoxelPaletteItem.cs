using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public abstract class UniversalVoxelPaletteItem : ScriptableObject, IVoxelPaletteItem
	{
		[SerializeField] string niceName;
		[SerializeField] Color color;

		public string Name => niceName.IsNullOrEmpty() ? name : niceName;
		public Color Color => color;

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