using MUtility;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VoxelSystem
{
	[System.Serializable]
	public struct MaterialSetup
	{
		[SerializeField] Material material;
		[SerializeField] string displayName;
		[SerializeField] Color displayColor;
		[SerializeField] Texture2D displayTexture;

		internal string DisplayName => displayName.IsNullOrEmpty() ? material.name : displayName;
		public Material Material => material;
		public Color DisplayColor => displayColor;
		public Texture2D DisplayTexture => displayTexture;
	}

	[CreateAssetMenu(menuName = "Voxel System/Material Palette")]

	public class MaterialPalette : ScriptableObject
	{
		[SerializeField] List<MaterialSetup> materials;

		public MaterialSetup this[int index] => materials[index];

		public int Count => materials.Count;

		public IReadOnlyList<MaterialSetup> Items => materials;
	}
}