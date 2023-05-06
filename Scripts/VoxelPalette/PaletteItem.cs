using System;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public class PaletteItem : ScriptableObject
	{
		public string niceName;
		public Sprite image;
		public Color color;
	}
}