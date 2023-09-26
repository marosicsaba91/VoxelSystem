using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	public readonly struct OctoBlock
	{
		public readonly Vector3Int doubleSize;
		public readonly OctoBlockType blockType;
		public readonly Vector3Int subVoxel;
		public readonly Axis3D axis;

		public OctoBlock(OctoBlockType blockType, Vector3Int subVoxel, Axis3D axis)
		{
			doubleSize = Vector3Int.one;
			this.subVoxel = subVoxel;
			this.blockType = blockType;
			this.axis = axis; 
		}

		public OctoBlock(OctoBlockType blockType, Vector3Int subVoxel)
		{
			doubleSize = Vector3Int.one;
			this.subVoxel = subVoxel;
			this.blockType = blockType;
			axis = default;
		}

		public Vector3 RealSize => (Vector3)doubleSize / 2f;

		public Vector3 Center(Vector3Int subVoxelIndex) =>
			(Vector3)subVoxelIndex * 0.5f + (Vector3)doubleSize * 0.25f;
	}
}