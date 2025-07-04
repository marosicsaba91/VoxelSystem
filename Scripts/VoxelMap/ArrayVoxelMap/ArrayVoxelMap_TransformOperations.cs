﻿using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	public partial class ArrayVoxelMap
	{
		public sealed override void Turn(Axis3D axis, bool leftHandPositive)
		{
			int newW =
				axis == Axis3D.X ? size.x :
				axis == Axis3D.Y ? size.z :
				axis == Axis3D.Z ? size.y : 0;
			int newH =
				axis == Axis3D.X ? size.z :
				axis == Axis3D.Y ? size.y :
				axis == Axis3D.Z ? size.x : 0;
			int newD =
				axis == Axis3D.X ? size.y :
				axis == Axis3D.Y ? size.x :
				axis == Axis3D.Z ? size.z : 0;

			Voxel[] newVoxelData = new Voxel[voxelData.Length];

			for (int i = 0; i < voxelData.Length; i++)
			{
				Vector3Int original = GetCoordinate(i);
				int nx =
					axis == Axis3D.X ? original.x :
					axis == Axis3D.Y ? (leftHandPositive ? original.z : size.z - original.z - 1) :
					axis == Axis3D.Z ? (leftHandPositive ? size.y - original.y - 1 : original.y) : 0;
				int ny =
					axis == Axis3D.X ? (leftHandPositive ? size.z - original.z - 1 : original.z) :
					axis == Axis3D.Y ? original.y :
					axis == Axis3D.Z ? (leftHandPositive ? original.x : size.x - original.x - 1) : 0;
				int nz =
					axis == Axis3D.X ? (leftHandPositive ? original.y : size.y - original.y - 1) :
					axis == Axis3D.Y ? (leftHandPositive ? size.x - original.x - 1 : original.x) :
					axis == Axis3D.Z ? original.z : 0;

				int ni = nx + (ny * newW) + (nz * newW * newH);

				Voxel voxel = voxelData[i];
				CubicTransformation transformation = voxel.CubicTransformation;
				transformation.Turn(axis, leftHandPositive);
				voxel.CubicTransformation = transformation;
				newVoxelData[ni] = voxel;
			}

			size = new Vector3Int(newW, newH, newD);
			voxelData = newVoxelData;
		}

		public sealed override void Mirror(Axis3D axis)
		{
			Voxel[] newVoxelData = new Voxel[voxelData.Length];

			for (int i = 0; i < voxelData.Length; i++)
			{
				Vector3Int o = GetCoordinate(i);
				if (axis == Axis3D.X)
					o.x = size.x - o.x - 1;
				if (axis == Axis3D.Y)
					o.y = size.y - o.y - 1;
				if (axis == Axis3D.Z)
					o.z = size.z - o.z - 1;
				int ni = o.x + (o.y * size.x) + (o.z * size.x * size.y);

				Voxel voxel = voxelData[i];
				CubicTransformation transformation = voxel.CubicTransformation;
				transformation.Mirror(axis);
				voxel.CubicTransformation = transformation;
				newVoxelData[ni] = voxel;
			}
			voxelData = newVoxelData;
		}

		public sealed override void Resize(GeneralDirection3D direction, int steps)
		{
			Vector3Int newSize = (size + direction.ToVectorInt().Abs() * steps).Abs();
			Voxel[] newVoxelData = new Voxel[newSize.x * newSize.y * newSize.z];

			for (int i = 0; i < newVoxelData.Length; i++)
			{
				int oldIndex;
				int nx = i;
				int nz = nx / (newSize.x * newSize.y);
				nx -= nz * (newSize.x * newSize.y);
				int ny = nx / newSize.x;
				nx -= ny * (newSize.x);

				int ox = Mathf.Clamp((int)((float)nx / newSize.x * size.x), 0, size.x - 1);
				int oy = Mathf.Clamp((int)((float)ny / newSize.y * size.y), 0, size.y - 1);
				int oz = Mathf.Clamp((int)((float)nz / newSize.z * size.z), 0, size.z - 1);
				oldIndex = Index(ox, oy, oz);

				if (oldIndex < 0 || oldIndex >= voxelData.Length)
				{
					Debug.Log("W: " + size.x + " -> " + newSize.x);
					Debug.Log("X: " + ox + " -> " + nx);

					Debug.Log("H: " + size.y + " -> " + newSize.y);
					Debug.Log("Y: " + oy + " -> " + ny);

					Debug.Log("D: " + size.z + " -> " + newSize.z);
					Debug.Log("Z: " + oz + " -> " + nz);
				}

				if (oldIndex < 0)
					newVoxelData[i] = Voxel.emptyValue;
				else
					newVoxelData[i] = voxelData[oldIndex];
			}

			size = newSize;
			voxelData = newVoxelData;
		}

		public sealed override void ResizeCanvas(GeneralDirection3D direction, int steps, bool repeat)
		{
			Vector3Int newSize = (size + direction.ToVectorInt().Abs() * steps).Abs();
			Voxel[] newVoxelData = new Voxel[newSize.x * newSize.y * newSize.z];

			for (int i = 0; i < newVoxelData.Length; i++)
			{
				int oldIndex;
				int ox = i;
				int oz = ox / (newSize.x * newSize.y);
				ox -= oz * (newSize.x * newSize.y);
				int oy = ox / newSize.x;
				ox -= oy * (newSize.x);

				if (direction == GeneralDirection3D.Left)
				{ ox -= (newSize.x - size.x); }
				if (direction == GeneralDirection3D.Down)
				{ oy -= (newSize.y - size.y); }
				if (direction == GeneralDirection3D.Back)
				{ oz -= (newSize.z - size.z); }

				if (ox >= size.x || oy >= size.y || oz >= size.z || ox < 0 || oy < 0 || oz < 0)
				{
					if (repeat)
					{
						oldIndex = Index(
							MathHelper.ModuloPositive(ox, size.x),
							MathHelper.ModuloPositive(oy, size.y),
							MathHelper.ModuloPositive(oz, size.z));
					}
					else 
						oldIndex = -1;
				}
				else
					oldIndex = Index(ox, oy, oz);
				if (oldIndex < 0)
					newVoxelData[i]= Voxel.emptyValue;
				else
					newVoxelData[i] = voxelData[oldIndex];
			}

			size = newSize;
			voxelData = newVoxelData;
		}
	}
}
