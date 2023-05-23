using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public abstract class VoxelPaletteSetting<TPalette, TItem> : IVoxelPalette<TItem> where TPalette : IVoxelPalette<TItem> where TItem : IVoxelPaletteItem
	{
		enum VoxelPaletteItemType { Single, Multiple}

		[SerializeField] VoxelPaletteItemType type;
		[SerializeField, HideIf(nameof(IsSingle))] TPalette palette;
		[SerializeField, ShowIf(nameof(IsSingle))] TItem voxelSetting;

		protected bool IsSingle => type == VoxelPaletteItemType.Single;	

		public IEnumerable<TItem> Items 
		{
			get
			{
				if (IsSingle)
					yield return voxelSetting;
				else if (palette == null)
					yield break;
				else
				{
					foreach (TItem item in palette.Items)
						yield return item;
				}
			} 
		}

		public int Length => IsSingle ? 1 : (palette == null ? 0 : palette.Length);
	}
}