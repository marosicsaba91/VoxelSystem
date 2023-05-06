using MUtility;
using System;
using UnityEngine;

namespace VoxelSystem
{
	partial class ArrayVoxelMap
	{
		protected sealed override bool Raycast(Ray localRay, out VoxelHit hit, bool returnOutsideVoxel = false)
		{
			// Try Find the entry point
			if (FindEntryPointToVoxelMap(localRay, out VoxelHit voxelMapEntry, FullSize))
				return RaycastInside(voxelMapEntry, out hit, localRay.direction, this, returnOutsideVoxel);

			hit = default;
			return false;
		}

		static bool FindEntryPointToVoxelMap(Ray ray, out VoxelHit hit, Vector3Int mapSize)
		{
			GeneralDirection3D[] sides = DirectionUtility.generalDirection3DValues;

			GeneralDirection3D entrySide;
			Vector3Int firstFoundVoxel;
			Vector3Int sideNormal;

			for (int i = 0; i < sides.Length; i++)
			{
				entrySide = sides[i];
				sideNormal = entrySide.ToVectorInt();
				bool positive = sideNormal.x > 0 || sideNormal.y > 0 || sideNormal.z > 0;
				Vector3 planeOrigin = positive ? Vector3.zero : mapSize;

				if (!ray.IntersectPlaneFast(sideNormal, planeOrigin, out Vector3 entryPoint))
					continue;

				if (!positive)
				{
					entryPoint = (mapSize + entryPoint);
					firstFoundVoxel = new((int)entryPoint.x, (int)entryPoint.y,
						(int)entryPoint.z);
					firstFoundVoxel += sideNormal;
				}
				else
				{
					firstFoundVoxel = new((int)entryPoint.x, (int)entryPoint.y,
						(int)entryPoint.z);
				}

				//
				if (mapSize.x < firstFoundVoxel.x || firstFoundVoxel.x < 0)
					continue;
				if (mapSize.y < firstFoundVoxel.y || firstFoundVoxel.y < 0)
					continue;
				if (mapSize.z < firstFoundVoxel.z || firstFoundVoxel.z < 0)
					continue;

				const float epsilon = 0.001f;
				if (entryPoint.x <= -epsilon || entryPoint.x >= mapSize.x + epsilon)
					continue;
				if (entryPoint.y <= -epsilon || entryPoint.y >= mapSize.y + epsilon)
					continue;
				if (entryPoint.z <= -epsilon || entryPoint.z >= mapSize.z + epsilon)
					continue;

				hit = new VoxelHit()
				{
					voxelIndex = firstFoundVoxel,
					hitWorldPosition = entryPoint,
					side = entrySide.Opposite()
				};
				return true;
			}

			hit = default;
			return false;
		}

		static bool RaycastInside(VoxelHit entry, out VoxelHit hit, Vector3 rayDirection, ArrayVoxelMap map, bool returnOutsideVoxel)
		{			 
			Vector3Int voxelIndex = entry.voxelIndex;
			
			if (!map.IsValidCoord(voxelIndex))
			{
				hit = entry;
				return false;
			}

			if (map.GetVoxel(voxelIndex).IsFilled())
			{
				hit = entry;
				return true;
			}

			VoxelHit cursor = new();
			// In the Cube
			bool xIsPositive = rayDirection.x > 0;
			bool yIsPositive = rayDirection.y > 0;
			bool zIsPositive = rayDirection.z > 0;

			Vector3Int dirSign = new (rayDirection.x > 0 ? 1 : -1, rayDirection.y > 0 ? 1 : -1, rayDirection.z > 0 ? 1 : -1);

			Vector3Int lastFoundVoxel = entry.voxelIndex;
			Vector3 lastIntersect = entry.hitWorldPosition;

			while (true)
			{
				cursor.hitWorldPosition = lastIntersect;
				cursor.voxelIndex = lastFoundVoxel;

				//cursorPathVoxels.Add(lastFoundVoxel);

				var stepDistance = new Vector3(
					xIsPositive ? Ceil(lastIntersect.x) - lastIntersect.x : Floor(lastIntersect.x) - lastIntersect.x,
					yIsPositive ? Ceil(lastIntersect.y) - lastIntersect.y : Floor(lastIntersect.y) - lastIntersect.y,
					zIsPositive ? Ceil(lastIntersect.z) - lastIntersect.z : Floor(lastIntersect.z) - lastIntersect.z
				);
				var distanceToIntersect = new Vector3(
					stepDistance.x / rayDirection.x,
					stepDistance.y / rayDirection.y,
					stepDistance.z / rayDirection.z);

				float minDistance = Mathf.Min(distanceToIntersect.x, distanceToIntersect.y, distanceToIntersect.z);
				if (Math.Abs(minDistance - distanceToIntersect.x) < epsilon)
				{
					lastFoundVoxel.x += dirSign.x;
					cursor.side = xIsPositive ? GeneralDirection3D.Right : GeneralDirection3D.Left;
				}
				else if (Math.Abs(minDistance - distanceToIntersect.y) < epsilon)
				{
					lastFoundVoxel.y += dirSign.y;
					cursor.side = yIsPositive ? GeneralDirection3D.Up : GeneralDirection3D.Down;
				}
				else if (Math.Abs(minDistance - distanceToIntersect.z) < epsilon)
				{
					lastFoundVoxel.z += dirSign.z;
					cursor.side = zIsPositive ? GeneralDirection3D.Forward : GeneralDirection3D.Back;
				}

				lastIntersect += minDistance * rayDirection;

				if (!map.IsValidCoord(lastFoundVoxel))
				{
					cursor.voxelIndex = returnOutsideVoxel ? cursor.voxelIndex : cursor.voxelIndex + cursor.side.ToVectorInt();
					cursor.side = returnOutsideVoxel ? cursor.side : cursor.side.Opposite();
					hit = cursor;
					return true;
				}

				if (map.GetVoxel(lastFoundVoxel.x, lastFoundVoxel.y, lastFoundVoxel.z).IsFilled())
				{
					cursor.voxelIndex = returnOutsideVoxel ? cursor.voxelIndex : cursor.voxelIndex + cursor.side.ToVectorInt();
					cursor.side = returnOutsideVoxel ? cursor.side : cursor.side.Opposite();
					hit = cursor;
					return true;
				}
			}
		}

		static int Ceil(float f)
		{
			if (f % 1f > 1f - epsilon)
				return ((int)f) + 2; 
			return ((int)f) + 1;
		}

		static int Floor(float f)
		{
			if (f % 1f < epsilon)
				return ((int)f) - 1;
			return (int)f;
		}

		const float epsilon = 0.0001f;
	}
}
