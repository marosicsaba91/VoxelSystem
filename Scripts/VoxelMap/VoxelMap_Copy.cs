﻿using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	partial class VoxelMap
	{
		public virtual void SetupFrom(VoxelMap map)
		{
			BoundsInt bounds = map.VoxelBoundaries;
			Setup(map.FullSize);
			foreach (Vector3Int index in bounds.WalkThrough())
				SetVoxel(index, map.GetVoxel(index));
		}

		public virtual void CopyFrom(
			VoxelMap sourceMap, 
			Vector3Int startCoordinateOfSourceMap,
			Vector3Int startCoordinateOfDestinationMap,
			Vector3Int copySize,
			VoxelAction action = VoxelAction.Attach)
		{
			bool mapChanged = false;

			for (int x = 0; x < copySize.x; x++)
			{
				int destinationX = startCoordinateOfDestinationMap.x + x;
				int sourceX = startCoordinateOfSourceMap.x + x;

				if (destinationX >= FullSize.x || destinationX < 0) continue;
				if (sourceX >= sourceMap.FullSize.x || sourceX < 0) continue;

				for (int y = 0; y < copySize.y; y++)
				{
					int destinationY = startCoordinateOfDestinationMap.y + y;
					int sourceY = startCoordinateOfSourceMap.y + y;

					if (destinationY >= FullSize.y || destinationY < 0) continue;
					if (sourceY >= sourceMap.FullSize.y || sourceY < 0) continue;

					for (int z = 0; z < copySize.z; z++)
					{
						int destinationZ = startCoordinateOfDestinationMap.z + z;
						int sourceZ = startCoordinateOfSourceMap.z + z;

						if (destinationZ >= FullSize.z || destinationZ < 0) continue;
						if (sourceZ >= sourceMap.FullSize.z || sourceZ < 0) continue;

						// TryGetCopy Voxel
						Voxel val = sourceMap.GetVoxel(sourceX, sourceY, sourceZ);
						mapChanged |= SetVoxel(destinationX, destinationY, destinationZ, action, val);
					}
				}
			}
		}
	}
}
