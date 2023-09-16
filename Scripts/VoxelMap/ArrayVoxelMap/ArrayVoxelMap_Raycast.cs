using MUtility;
using System;
using UnityEngine;

namespace VoxelSystem
{
	public partial class ArrayVoxelMap
	{
		protected sealed override bool Raycast(Ray localRay, out VoxelHit hit, bool returnOutsideVoxel = false)
		{
			// Try Find the entry point
			if (FindEntryPointToVoxelMap(localRay, out VoxelHit voxelMapEntry, FullSize))
				return RaycastInside(voxelMapEntry, out hit, localRay, this, returnOutsideVoxel);


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

				if (positive)
				{
					firstFoundVoxel = new((int)entryPoint.x, (int)entryPoint.y,
						(int)entryPoint.z);
				}
				else
				{
					entryPoint = (mapSize + entryPoint);
					firstFoundVoxel = new((int)entryPoint.x, (int)entryPoint.y,
						(int)entryPoint.z);
					firstFoundVoxel += sideNormal;
				}

				// 
				if (mapSize.x < firstFoundVoxel.x || firstFoundVoxel.x < 0) continue;
				if (mapSize.y < firstFoundVoxel.y || firstFoundVoxel.y < 0) continue;
				if (mapSize.z < firstFoundVoxel.z || firstFoundVoxel.z < 0) continue;

				const float epsilon = 0.001f;
				if (entryPoint.x <= -epsilon || entryPoint.x >= mapSize.x + epsilon) continue;
				if (entryPoint.y <= -epsilon || entryPoint.y >= mapSize.y + epsilon) continue;
				if (entryPoint.z <= -epsilon || entryPoint.z >= mapSize.z + epsilon) continue;

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

		static bool RaycastInside(
			VoxelHit entry, out VoxelHit hit,
			Ray ray, ArrayVoxelMap map,
			bool returnOutsideVoxel)
		{
			Vector3 rayDirection = ray.direction;
			Vector3Int voxelIndex = entry.voxelIndex;
			
			if (!map.IsValidCoord(voxelIndex))
			{
				hit = entry;
				return false;
			}

			if (map.GetVoxel(voxelIndex).IsFilled() && IsBeforeOrigin(ray, voxelIndex))
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

			const int maxIterations = 100000;


			for (int i = 0; i< maxIterations; i++)
			{
				cursor.hitWorldPosition = lastIntersect;
				cursor.voxelIndex = lastFoundVoxel;

				//cursorPathVoxels.Add(lastFoundVoxel);

				Vector3 stepDistance = new (
					xIsPositive ? Ceil(lastIntersect.x) - lastIntersect.x : Floor(lastIntersect.x) - lastIntersect.x,
					yIsPositive ? Ceil(lastIntersect.y) - lastIntersect.y : Floor(lastIntersect.y) - lastIntersect.y,
					zIsPositive ? Ceil(lastIntersect.z) - lastIntersect.z : Floor(lastIntersect.z) - lastIntersect.z);

				Vector3 distanceToIntersect = new (
					rayDirection.x == 0 ? float.MaxValue : (stepDistance.x / rayDirection.x),
					rayDirection.y == 0 ? float.MaxValue : (stepDistance.y / rayDirection.y),
					rayDirection.z == 0 ? float.MaxValue : (stepDistance.z / rayDirection.z));

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
					cursor.hitWorldPosition = lastIntersect;
					hit = cursor;
					return true;
				}

				if (map.GetVoxel(lastFoundVoxel).IsFilled() && IsBeforeOrigin(ray, lastFoundVoxel))
				{
					cursor.voxelIndex = returnOutsideVoxel ? cursor.voxelIndex : cursor.voxelIndex + cursor.side.ToVectorInt();
					cursor.side = returnOutsideVoxel ? cursor.side : cursor.side.Opposite();
					cursor.hitWorldPosition = lastIntersect;
					hit = cursor;
					return true;
				}
			}
			Debug.LogError("Max Iterations Reached!");
			hit = default;
			return false;

		}

		static bool IsBeforeOrigin(Ray ray, Vector3 point) 
		{
			Vector3 originToPoint = point - ray.origin;
			return Vector3.Dot(ray.direction, originToPoint) > 0;		
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
