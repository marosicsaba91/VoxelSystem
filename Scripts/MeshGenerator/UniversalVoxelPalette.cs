using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "Universal Voxel Palette", menuName = "Voxel System/Universal Voxel Palette", order = 1)]
	public class UniversalVoxelPalette : ScriptableObject, IPalette
	{
		[SerializeField] List<UniversalVoxelBuilder> _items;
		public IReadOnlyList<UniversalVoxelBuilder> VoxelTypes => _items;
		public IReadOnlyList<IPaletteItem> PaletteItems => _items;
	}

	public abstract class UniversalVoxelBuilder : ScriptableObject, IPaletteItem
	{
		[SerializeField] string niceName;
		[SerializeField] Color color;
		[SerializeField] UniversalVoxelBuilder quickVersion;

		private void OnValidate()
		{
			if (quickVersion != null)
			{
				if (quickVersion.quickVersion != null)
				{
					quickVersion.quickVersion = null;
					Debug.LogWarning("Quick version can not have a quick version");
				}
				if (quickVersion == this)
				{
					quickVersion = null;
					Debug.LogWarning("Quick version can not be self");
				}
			}
		}

		public string DisplayName => niceName.IsNullOrEmpty() ? name : niceName;

		public Color DisplayColor => color;

		UniversalVoxelBuilder GetVoxelVersion(bool quick) => (quick && quickVersion != null) ? quickVersion : this;

		internal void BeforeMeshGeneration(VoxelMap map, UniversalVoxelPalette palette, int voxelTypeIndex, bool quick) =>
			GetVoxelVersion(quick).BeforeMeshGeneration(map, palette, voxelTypeIndex);


		protected abstract void BeforeMeshGeneration(VoxelMap map, UniversalVoxelPalette palette, int voxelTypeIndex);

		internal void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> palette,
			int voxelTypeIndex,
			List<Vector3> vertexOut,
			List<Vector3> normalOut,
			List<Vector2> uvOut,
			List<int> triangleOut,
			bool quick) =>
			GetVoxelVersion(quick).GenerateMeshData(map, palette, voxelTypeIndex, vertexOut, normalOut, uvOut, triangleOut);

		protected abstract void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int voxelTypeIndex,
			List<Vector3> vertexOut,
			List<Vector3> normalOut,
			List<Vector2> uvOut,
			List<int> triangleOut);
	}
}