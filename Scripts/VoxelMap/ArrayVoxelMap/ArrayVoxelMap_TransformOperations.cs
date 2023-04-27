using MUtility;
using System;
using UnityEngine;

namespace VoxelSystem
{
	public partial class ArrayVoxelMap
	{

		public void Turn(Axis3D axis, bool leftHandPositive)
		{
			int newW =
				axis == Axis3D.X ? Width :
				axis == Axis3D.Y ? Depth :
				axis == Axis3D.Z ? Height : 0;
			int newH =
				axis == Axis3D.X ? Depth :
				axis == Axis3D.Y ? Height :
				axis == Axis3D.Z ? Width : 0;
			int newD =
				axis == Axis3D.X ? Height :
				axis == Axis3D.Y ? Width :
				axis == Axis3D.Z ? Depth : 0;

			var newVoxelData = new Voxel[voxelData.Length];

			for (int i = 0; i < voxelData.Length; i++)
			{
				Vector3Int original = Index(i);
				int nx =
					axis == Axis3D.X ? original.x :
					axis == Axis3D.Y ? (leftHandPositive ? depth - original.z - 1 : original.z) :
					axis == Axis3D.Z ? (leftHandPositive ? original.y : height - original.y - 1) : 0;
				int ny =
					axis == Axis3D.X ? (leftHandPositive ? original.z : depth - original.z - 1) :
					axis == Axis3D.Y ? original.y :
					axis == Axis3D.Z ? (leftHandPositive ? width - original.x - 1 : original.x) : 0;
				int nz =
					axis == Axis3D.X ? (leftHandPositive ? height - original.y - 1 : original.y) :
					axis == Axis3D.Y ? (leftHandPositive ? original.x : width - original.x - 1) :
					axis == Axis3D.Z ? original.z : 0;

				//Debug.Log(original+"  "+new Vector3Int(nx,ny,nz));
				int ni = nx + (ny * newW) + (nz * newW * newH);
				newVoxelData[ni] = voxelData[i];
			}

			width = newW;
			height = newH;
			depth = newD;
			voxelData = newVoxelData;
			MapChanged();
		}

		public void Mirror(Axis3D axis)
		{
			var newVoxelData = new Voxel[voxelData.Length];

			for (int i = 0; i < voxelData.Length; i++)
			{
				Vector3Int o = Index(i);
				if (axis == Axis3D.X)
					o.x = width - o.x - 1;
				if (axis == Axis3D.Y)
					o.y = height - o.y - 1;
				if (axis == Axis3D.Z)
					o.z = depth - o.z - 1;
				int ni = o.x + (o.y * width) + (o.z * width * height);

				newVoxelData[ni] = voxelData[i];
			}
			voxelData = newVoxelData;
			MapChanged();
		}
		public void Resize(GeneralDirection3D direction, int steps, ResizeType type)
		{
			Axis3D axis = direction.GetAxis();
			int newW = (axis == Axis3D.X) ? Math.Max(val1: 1, width + steps) : width;
			int newH = (axis == Axis3D.Y) ? Math.Max(val1: 1, height + steps) : height;
			int newD = (axis == Axis3D.Z) ? Math.Max(val1: 1, depth + steps) : depth;

			var newVoxelData = new Voxel[newW * newH * newD];

			for (int i = 0; i < newVoxelData.Length; i++)
			{
				int oldIndex;
				if (type == ResizeType.Rescale)
				{
					int nx = i;
					int nz = nx / (newW * newH);
					nx -= nz * (newW * newH);
					int ny = nx / newW;
					nx -= ny * (newW);
					int ox = Mathf.Clamp((int)((float)nx / newW * width), min: 0, width - 1);
					int oy = Mathf.Clamp((int)((float)ny / newH * height), min: 0, height - 1);
					int oz = Mathf.Clamp((int)((float)nz / newD * depth), min: 0, depth - 1);
					oldIndex = Index(ox, oy, oz);
					if (oldIndex < 0 || oldIndex >= voxelData.Length)
					{
						Debug.Log("W: " + width + " -> " + newW);
						Debug.Log("X: " + ox + " -> " + nx);

						Debug.Log("H: " + height + " -> " + newH);
						Debug.Log("Y: " + oy + " -> " + ny);

						Debug.Log("D: " + depth + " -> " + newD);
						Debug.Log("Z: " + oz + " -> " + nz);
					}
				}
				else
				{
					int ox = i;
					int oz = ox / (newW * newH);
					ox -= oz * (newW * newH);
					int oy = ox / newW;
					ox -= oy * (newW);

					if (direction == GeneralDirection3D.Left)
					{ ox -= (newW - width); }
					if (direction == GeneralDirection3D.Down)
					{ oy -= (newH - height); }
					if (direction == GeneralDirection3D.Back)
					{ oz -= (newD - depth); }

					if (ox >= width || oy >= height || oz >= depth || ox < 0 || oy < 0 || oz < 0)
					{
						if (type == ResizeType.Repeat)
						{
							oldIndex = Index(MathHelper.Mod(ox, width), MathHelper.Mod(oy, height), MathHelper.Mod(oz, depth));
						}
						else
						{ oldIndex = -1; }
					}
					else
					{
						oldIndex = Index(ox, oy, oz);
					}
				}

				if (oldIndex < 0)
					newVoxelData[i] = new Voxel(value: -1);
				else
					newVoxelData[i] = voxelData[oldIndex];
			}

			width = newW;
			height = newH;
			depth = newD;
			voxelData = newVoxelData;
			MapChanged();
		}


	}
}
