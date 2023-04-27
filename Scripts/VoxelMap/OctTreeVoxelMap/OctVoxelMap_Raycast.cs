using MUtility;
using System;
using UnityEngine;

namespace VoxelSystem
{ 
	partial class OctVoxelMap
	{
		public bool Raycast(Ray ray, out VoxelHitPoint hit, Transform voxelTransform, bool returnOutsideVoxel = false)
		{
			if (voxelTransform == null)
			{
				hit = new VoxelHitPoint();
				return false;
			}
			Matrix4x4 matrix = voxelTransform.worldToLocalMatrix;
			return Raycast(ray, out hit, this, matrix, returnOutsideVoxel);
		}

		public bool Raycast(Ray ray, out VoxelHitPoint hit, Matrix4x4 voxelMatrix, bool returnOutsideVoxel = false) => Raycast(ray, out hit, this, voxelMatrix, returnOutsideVoxel);

		static bool Raycast(Ray globalRay, out VoxelHitPoint hit, OctVoxelMap map, Matrix4x4 matrix, bool returnOutsideVoxel)
		{
			Ray localRay = globalRay.Transform(matrix);

			// Try Find the entry point
			if (FindEntryPointToVoxelMap(localRay, out VoxelHitPoint voxelMapEntry, map.CanvasSize))
			{
				// We search for teh voxel on the line until We find a filled voxel
				return RaycastInside(voxelMapEntry, out hit, localRay.direction, map, returnOutsideVoxel);
			}
			hit = default;
			return false;
		}

		static bool FindEntryPointToVoxelMap(Ray ray, out VoxelHitPoint hit, Vector3Int mapSize)
		{
			GeneralDirection3D[] sides = DirectionUtility.generalDirection3DValues;

			GeneralDirection3D entrySide = GeneralDirection3D.Up;
			Vector3Int firstFoundVoxel = Vector3Int.zero;
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

				const float epsylon = 0.001f;
				if (entryPoint.x <= -epsylon || entryPoint.x >= mapSize.x + epsylon)
					continue;
				if (entryPoint.y <= -epsylon || entryPoint.y >= mapSize.y + epsylon)
					continue;
				if (entryPoint.z <= -epsylon || entryPoint.z >= mapSize.z + epsylon)
					continue;

				hit = new VoxelHitPoint()
				{
					voxel = firstFoundVoxel,
					point = entryPoint,
					side = entrySide.Opposite()
				};
				return true;
			}

			hit = default;
			return false;
		}

		// TODO: POSSIBLE OPTIMISATIONS
		static bool RaycastInside(VoxelHitPoint entry, out VoxelHitPoint hit, Vector3 rayDirection, OctVoxelMap map, bool returnOutsideVoxel)
		{
			if (!map.IsValidCoord(entry.voxel))  // NEM KELLENE
			{
				hit = default;
				return false;
			}

			// var cursorPathVoxels = new List<Vector3Int>();
			Vector3Int e = entry.voxel;
			if (map.GetVoxel(e.x, e.y, e.z).IsFilled())
			{
				hit = entry;
				return true;
			}

			VoxelHitPoint cursor = new();
			// In the Cube
			bool xIsPositive = rayDirection.x > 0;
			bool yIsPositive = rayDirection.y > 0;
			bool zIsPositive = rayDirection.z > 0;

			Vector3Int dirSign = new Vector3Int(rayDirection.x > 0 ? 1 : -1, rayDirection.y > 0 ? 1 : -1, rayDirection.z > 0 ? 1 : -1);

			Vector3Int lastFoundVoxel = entry.voxel;
			Vector3 lastIntersect = entry.point;

			while (true)
			{
				cursor.point = lastIntersect;
				cursor.voxel = lastFoundVoxel;

				//cursorPathVoxels.Add(lastFoundVoxel);

				var distanceToDo = new Vector3(
					xIsPositive ? Ceil(lastIntersect.x) - lastIntersect.x : Floor(lastIntersect.x) - lastIntersect.x,
					yIsPositive ? Ceil(lastIntersect.y) - lastIntersect.y : Floor(lastIntersect.y) - lastIntersect.y,
					zIsPositive ? Ceil(lastIntersect.z) - lastIntersect.z : Floor(lastIntersect.z) - lastIntersect.z
				);
				var distanceToIntersect = new Vector3(
					distanceToDo.x / rayDirection.x,
					distanceToDo.y / rayDirection.y,
					distanceToDo.z / rayDirection.z);

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
					cursor.voxel = returnOutsideVoxel ? cursor.voxel : cursor.voxel + cursor.side.ToVectorInt();
					cursor.side = returnOutsideVoxel ? cursor.side : cursor.side.Opposite();
					hit = cursor;
					return true;
				}

				if (map.GetVoxel(lastFoundVoxel.x, lastFoundVoxel.y, lastFoundVoxel.z).IsFilled())
				{
					cursor.voxel = returnOutsideVoxel ? cursor.voxel : cursor.voxel + cursor.side.ToVectorInt();
					cursor.side = returnOutsideVoxel ? cursor.side : cursor.side.Opposite();
					hit = cursor;
					return true;
				}
			}
		}


		static int Ceil(float f)
		{
			if (f % 1f > 1f - epsilon)
			{ return ((int)f) + 2; }
			return ((int)f) + 1;
		}

		static int Floor(float f)
		{
			if (f % 1f < epsilon)
			{ return ((int)f) - 1; }
			return (int)f;
		}

		const float epsilon = 0.0001f;
	}
}
