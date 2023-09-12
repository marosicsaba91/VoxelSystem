using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(menuName = "Voxel System/Material Palette")]

	public class MaterialPalette : ScriptableObject, IPalette
	{
		[SerializeField] List<MaterialSetup> materials;

		public MaterialSetup this[int index] => materials[index];

		public int Count => materials.Count;

		public IReadOnlyList<MaterialSetup> Materials => materials;

		public IReadOnlyList<IPaletteItem> PaletteItems => materials;
	}

	[System.Serializable]
	public class MaterialSetup : IPaletteItem
	{
		[SerializeField] Material material;
		[SerializeField] string displayName;
		[SerializeField] Color displayColor;
		[SerializeField] Texture2D displayTexture;

		public string DisplayName => displayName.IsNullOrEmpty() ? material.name : displayName;
		public Material Material => material;
		public Color DisplayColor => displayColor;
		public Texture2D DisplayTexture => displayTexture;
	}
}