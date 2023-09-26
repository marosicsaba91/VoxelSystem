using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{
	[Serializable]
	class BlockColorDictionary : SerializableDictionary<OctoBlockType, Color> { }

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

		public void DrawGizmos(VoxelMap map, List<(Vector3Int,OctoBlock)> _blocks)
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
				foreach ((Vector3Int, OctoBlock) block in _blocks)
				{
					Gizmos.color = drawingSettings.blockColors.TryGetValue(block.Item2.blockType, out Color color)
						? color
						: Color.magenta;
					DrawBlock(block.Item2, block.Item1, drawingSettings.margin, _gizmoRandom);
				}
			}
		}


		public void DrawBlock(OctoBlock block, Vector3Int subVoxelIndex, float margin, System.Random random)
		{
			if (block.blockType == OctoBlockType.SidePositive)
				DrawSide(block, subVoxelIndex, margin);
			else if (block.blockType == OctoBlockType.EdgePositive)
				DrawEdge(block, subVoxelIndex, margin);
			else if (block.blockType == OctoBlockType.CornerPositive)
				DrawCorner(block, subVoxelIndex, margin);
			else
				DrawAnything(block, subVoxelIndex, random);
		}

		void DrawCorner(OctoBlock block, Vector3Int subVoxelIndex, float margin)
		{
			Vector3 center = block.Center(subVoxelIndex);
			Vector3 realSize = block.RealSize - margin * Vector3.one;


			Vector3 normal = block.subVoxel;

			Vector3 c1 = center +
						 new Vector3(normal.x * 0.25f, normal.y * margin / 2f, normal.z * margin / 2f);
			Vector3 c2 = center +
						 new Vector3(normal.x * margin / 2f, normal.y * 0.25f, normal.z * margin / 2f);
			Vector3 c3 = center +
						 new Vector3(normal.x * margin / 2f, normal.y * margin / 2f, normal.z * 0.25f);

			Vector3 n1 = normal.MultiplyAllAxis(1, 0, 0);
			Vector3 n2 = normal.MultiplyAllAxis(0, 1, 0);
			Vector3 n3 = normal.MultiplyAllAxis(0, 0, 1);

			DrawRect(c1, realSize, n1);
			DrawRect(c2, realSize, n2);
			DrawRect(c3, realSize, n3);
		}

		void DrawEdge(OctoBlock block, Vector3Int subVoxelIndex, float margin)
		{
			// DrawAnything();
			// return;

			Vector3 c1, c2;

			Vector3 n1, n2;
			Vector3 realSize = block.RealSize;
			Vector3 center = block.Center(subVoxelIndex);

			Vector3 axisNeg = Vector3.one - block.axis.ToVector();
			Vector3 normal = block.subVoxel.MultiplyAllAxis(axisNeg);

			if (normal.x == 0)
			{
				c1 = center + new Vector3(0, normal.y * 0.25f, normal.z * margin / 2f);
				c2 = center + new Vector3(0, normal.y * margin / 2f, normal.z * 0.25f);
				n1 = normal.MultiplyAllAxis(0, 1, 0);
				n2 = normal.MultiplyAllAxis(0, 0, 1);
				realSize.x -= margin * 2;
				realSize.y -= margin;
				realSize.z -= margin;
			}
			else if (normal.y == 0)
			{
				c1 = center + new Vector3(normal.x * 0.25f, 0, normal.z * margin / 2f);
				c2 = center + new Vector3(normal.x * margin / 2f, 0, normal.z * 0.25f);
				n1 = normal.MultiplyAllAxis(1, 0, 0);
				n2 = normal.MultiplyAllAxis(0, 0, 1);
				realSize.x -= margin;
				realSize.y -= margin * 2;
				realSize.z -= margin;
			}
			else
			{
				c1 = center + new Vector3(normal.x * 0.25f, normal.y * margin / 2f, 0);
				c2 = center + new Vector3(normal.x * margin / 2f, normal.y * 0.25f, 0);
				n1 = normal.MultiplyAllAxis(1, 0, 0);
				n2 = normal.MultiplyAllAxis(0, 1, 0);
				realSize.x -= margin;
				realSize.y -= margin;
				realSize.z -= margin * 2;
			}

			DrawRect(c1, realSize, n1);
			DrawRect(c2, realSize, n2);
		}

		void DrawSide(OctoBlock block, Vector3Int subVoxelIndex, float margin)
		{
			Vector3 normal = block.axis.ToVector();
			Vector3 offset = normal.MultiplyAllAxis(block.subVoxel);

			Vector3 center = block.Center(subVoxelIndex) + offset * 0.25f;
			Vector3 realSize = block.RealSize - (2 * margin * Vector3.one);
			DrawRect(center, realSize, normal);
		}

		static void DrawRect(Vector3 center, Vector3 size, Vector3 normal)
		{
			// Gizmos.DrawWireSphere(center, 0.03f);
			// Gizmos.DrawLine(center , center + normal * 0.1f);

			Vector3 d1, d2;
			if (normal.x != 0)
			{
				d1 = Vector3.up * (size.y / 2f);
				d2 = Vector3.forward * (size.z / 2f);
			}
			else if (normal.y != 0)
			{
				d1 = Vector3.right * (size.x / 2f);
				d2 = Vector3.forward * (size.z / 2f);
			}
			else
			{
				d1 = Vector3.right * (size.x / 2f);
				d2 = Vector3.up * (size.y / 2f);
			}

			Gizmos.DrawLine(center - d1 + d2, center + d1 + d2);
			Gizmos.DrawLine(center - d1 - d2, center + d1 - d2);
			Gizmos.DrawLine(center - d1 + d2, center - d1 - d2);
			Gizmos.DrawLine(center + d1 + d2, center + d1 - d2);
		}

		void DrawAnything(OctoBlock block, Vector3Int subVoxelIndex, System.Random random)
		{
			Vector3 center = block.Center(subVoxelIndex);
			float rand = (float)random.NextDouble();
			rand *= 0.5f;
			rand += 1;
			float radius = 0.1f * rand;
			Gizmos.DrawWireSphere(center, radius);
			Vector3 center2 = center + (Vector3)block.subVoxel * (radius * 0.5f);
			Gizmos.DrawWireCube(center2, new Vector3(radius, radius, radius));


			if (block.blockType.HaveAxis())
			{
				Vector3 axisVector = block.axis.ToVector();
				Gizmos.DrawLine(center - axisVector * 0.25f, center + axisVector * 0.25f);
			}

			if (block.blockType == OctoBlockType.EdgeToEdge)
			{
				Axis3D connectedAxis = block.axis switch
				{
					Axis3D.X => Axis3D.Y,
					Axis3D.Y => Axis3D.Z,
					_ => Axis3D.X
				};

				Vector3 axisVector = connectedAxis.ToVector();
				Vector3 center3 = center + Vector3.up * 0.25f;
				Gizmos.DrawLine(center3 - axisVector * 0.15f, center3 + axisVector * 0.15f);
			}
		}
	}
}
