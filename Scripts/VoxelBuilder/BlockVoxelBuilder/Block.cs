using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	public struct Block
	{
		public static readonly Vector3 halfVoxel = new(0.5f, 0.5f, 0.5f);

		public readonly BlockType blockType;
		public Vector3Int doubleSize;

		public Vector3Int inVoxelDirection;
		public Axis3D axis;
		public Vector3 position; // Center of the first half voxel

		public Block(BlockType blockType, Vector3Int inVoxelDirection, Vector3Int doubleSize, Axis3D axis,
			Vector3 position)
		{
			this.blockType = blockType;
			this.doubleSize = doubleSize;
			this.position = position;
			this.axis = axis;
			this.inVoxelDirection = inVoxelDirection;
		}

		public Block(BlockType blockType, Vector3Int inVoxelDirection, Vector3Int doubleSize, Vector3 position)
		{
			this.blockType = blockType;
			this.doubleSize = doubleSize;
			this.position = position;
			this.inVoxelDirection = inVoxelDirection;
			axis = default;
		}



		public Vector3 RealSize => (Vector3)doubleSize / 2f;

		public Vector3 Center => position + RealSize / 2f;


		public void DrawGizmo(float margin, System.Random random)
		{
			if (blockType == BlockType.SidePositive)
				DrawSide(margin);
			else if (blockType == BlockType.EdgePositive)
				DrawEdge(margin);
			else if (blockType == BlockType.CornerPositive)
				DrawCorner(margin);
			else
				DrawAnything(random);
		}

		void DrawCorner(float margin)
		{
			Vector3 center = Center;
			Vector3 realSize = RealSize - margin * Vector3.one;


			Vector3 normal = inVoxelDirection;

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

		void DrawEdge(float margin)
		{
			// DrawAnything();
			// return;

			Vector3 c1, c2;

			Vector3 n1, n2;
			Vector3 realSize = RealSize;
			Vector3 center = Center;

			Vector3 axisNeg = Vector3.one - axis.ToVector();
			Vector3 normal = inVoxelDirection.MultiplyAllAxis(axisNeg);

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

		void DrawSide(float margin)
		{
			Vector3 normal = axis.ToVector();
			Vector3 offset = normal.MultiplyAllAxis(inVoxelDirection);

			Vector3 center = Center + offset * 0.25f;
			Vector3 realSize = RealSize - (2 * margin * Vector3.one);
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

		void DrawAnything(System.Random random)
		{
			Vector3 center = Center;
			var rand = (float)random.NextDouble();
			rand *= 0.5f;
			rand += 1;
			float radius = 0.1f * rand;
			Gizmos.DrawWireSphere(center, radius);
			Vector3 center2 = center + (Vector3)inVoxelDirection * (radius * 0.5f);
			Gizmos.DrawWireCube(center2, new Vector3(radius, radius, radius));


			if (blockType.HaveAxis())
			{
				Vector3 axisVector = axis.ToVector();
				Gizmos.DrawLine(center - axisVector * 0.25f, center + axisVector * 0.25f);
			}

			if (blockType == BlockType.EdgeToEdge)
			{
				Axis3D connectedAxis = axis switch
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