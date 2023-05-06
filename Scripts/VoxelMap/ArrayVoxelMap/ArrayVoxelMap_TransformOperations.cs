using MUtility;
using System;
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

			int[] newVoxelData = new int[intVoxelData.Length];

			for (int i = 0; i < intVoxelData.Length; i++)
			{
				Vector3Int original = Index(i);
				int nx =
					axis == Axis3D.X ? original.x :
					axis == Axis3D.Y ? (leftHandPositive ? size.z - original.z - 1 : original.z) :
					axis == Axis3D.Z ? (leftHandPositive ? original.y : size.y - original.y - 1) : 0;
				int ny =
					axis == Axis3D.X ? (leftHandPositive ? original.z : size.z - original.z - 1) :
					axis == Axis3D.Y ? original.y :
					axis == Axis3D.Z ? (leftHandPositive ? size.x - original.x - 1 : original.x) : 0;
				int nz =
					axis == Axis3D.X ? (leftHandPositive ? size.y - original.y - 1 : original.y) :
					axis == Axis3D.Y ? (leftHandPositive ? original.x : size.x - original.x - 1) :
					axis == Axis3D.Z ? original.z : 0;

				//Debug.Log(original+"  "+new Vector3Int(nx,ny,nz));
				int ni = nx + (ny * newW) + (nz * newW * newH);
				newVoxelData[ni] = intVoxelData[i];
			}

			size = new Vector3Int(newW, newH, newD);
			intVoxelData = newVoxelData;
		}

		public sealed override void Mirror(Axis3D axis)
		{
			int[] newVoxelData = new int[intVoxelData.Length];

			for (int i = 0; i < intVoxelData.Length; i++)
			{
				Vector3Int o = Index(i);
				if (axis == Axis3D.X)
					o.x = size.x - o.x - 1;
				if (axis == Axis3D.Y)
					o.y = size.y - o.y - 1;
				if (axis == Axis3D.Z)
					o.z = size.z - o.z - 1;
				int ni = o.x + (o.y * size.x) + (o.z * size.x * size.y);

				newVoxelData[ni] = intVoxelData[i];
			}
			intVoxelData = newVoxelData;
		}

		public sealed override void Resize(GeneralDirection3D direction, int steps)
		{
			Axis3D axis = direction.GetAxis();
			Vector3Int newSize = (size + direction.ToVectorInt() * steps).Abs();
			int[] newVoxelData = new int[newSize.x * newSize.y * newSize.z];

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

				if (oldIndex < 0 || oldIndex >= intVoxelData.Length)
				{
					Debug.Log("W: " + size.x + " -> " + newSize.x);
					Debug.Log("X: " + ox + " -> " + nx);

					Debug.Log("H: " + size.y + " -> " + newSize.y);
					Debug.Log("Y: " + oy + " -> " + ny);

					Debug.Log("D: " + size.z + " -> " + newSize.z);
					Debug.Log("Z: " + oz + " -> " + nz);
				}

				if (oldIndex < 0)
					newVoxelData[i] = IntVoxelUtility.emptyValue;
				else
					newVoxelData[i] = intVoxelData[oldIndex];
			}

			size = newSize;
			intVoxelData = newVoxelData;
		}

		public sealed override void ResizeCanvas(GeneralDirection3D direction, int steps, bool repeat)
		{
			Vector3Int newSize = (size + direction.ToVectorInt() * steps).Abs();
			int[] newVoxelData = new int[newSize.x * newSize.y * newSize.z];

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
							MathHelper.Mod(ox, size.x),
							MathHelper.Mod(oy, size.y),
							MathHelper.Mod(oz, size.z));
					}
					else 
						oldIndex = -1;
				}
				else
					oldIndex = Index(ox, oy, oz);
				if (oldIndex < 0)
					newVoxelData[i] = IntVoxelUtility.emptyValue;
				else
					newVoxelData[i] = intVoxelData[oldIndex];
			}

			size = newSize;
			intVoxelData = newVoxelData;
		}
	}
}
