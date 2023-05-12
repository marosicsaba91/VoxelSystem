using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public struct VoxelPaletteSetting : IVoxelPalette
	{
		enum VoxelPaletteItemType { Single, Multiple}

		[SerializeField] VoxelPaletteItemType type;
		[SerializeField, HideIf(nameof(IsSingle))] VoxelPalette palette;
		[SerializeField, ShowIf(nameof(IsSingle))] VoxelPaletteItem voxelSetting;

		bool IsSingle => type == VoxelPaletteItemType.Single;	

		public IEnumerable<VoxelPaletteItem> Items 
		{
			get
			{
				if (IsSingle)
					yield return voxelSetting;
				else if (palette == null)
					yield break;
				else
				{
					foreach (VoxelPaletteItem item in palette.Items)
						yield return item;
				}
			} 
		}

		public int Length => IsSingle ? 1 : (palette == null ? 0 : palette.Length);
	}
}