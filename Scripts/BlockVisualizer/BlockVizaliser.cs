using System;
using System.Collections.Generic;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{
	[Serializable]
	class BlockColorDictionary : SerializableDictionary<BlockType, Color> { }

	[Serializable]
	class BlockDrawingSettings
	{
		public bool drawVoxels = true;
		public Color voxelGizmoColor = new(1, 1, 1, 0.25f);
		public bool drawBlocks = true;
		public BlockColorDictionary blockColors = new();
		[Range(0, 0.25f)] public float margin = 0.1f;
	}

	class BlockVisualizer : MonoBehaviour
	{
		[SerializeField] BlockDrawingSettings drawingSettings = new();
		[SerializeField] int randomSeed = 0;


		static System.Random _gizmoRandom;

		public void DrawGizmos(VoxelMap map, List<Block> _blocks)
		{
			// Draw whole voxel map
			if (drawingSettings.drawVoxels)
			{
				Gizmos.color = drawingSettings.voxelGizmoColor;
				Vector3Int size = map.FullSize;
				for (int x = size.x - 1; x >= 0; x--)
					for (int y = size.y - 1; y >= 0; y--)
						for (int z = size.z - 1; z >= 0; z--)
						{
							if (map.GetVoxel(x, y, z).IsFilled())
								Gizmos.DrawWireCube(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), Vector3.one);
						}
			}

			// Draw Blocks
			if (drawingSettings.drawBlocks)
			{ 
				_gizmoRandom = new System.Random(randomSeed);
				foreach (Block block in _blocks)
				{
					Gizmos.color = drawingSettings.blockColors.TryGetValue(block.blockType, out Color color)
						? color
						: Color.magenta;
					block.DrawGizmo(drawingSettings.margin, _gizmoRandom);
				}
			}
		}
	}
}
