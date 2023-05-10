using System;
using System.Linq;
using UnityEngine;
using MUtility;

namespace VoxelSystem
{
	[Serializable]
	public partial class ArrayVoxelMap : VoxelMap
	{
		public const int defaultMapSize = 8;

		[SerializeField] Vector3Int size;
		[SerializeField] int[] intVoxelData;

		public override Vector3Int FullSize
		{
			get => size;
			protected set // Reset Whole Map
			{
				bool isSizeInvalid = value.x <= 0 || value.y <= 0 || value.z <= 0;
				if (isSizeInvalid)
					throw new ArgumentException("Size must be positive");
				if (intVoxelData != null && size == value) return;

				size = value;
				intVoxelData = new int[size.x * size.y * size.z];
			}
		}

		// Constructor -----------------------------------------------------------------------------

		public ArrayVoxelMap() { SetupUniqueID(); }

		public ArrayVoxelMap(Vector3Int size, int value = IntVoxelUtility.emptyValue)
		{
			SetupUniqueID();
			Setup(size, value);
		}

		public sealed override void Setup() => Setup(defaultMapSize * Vector3Int.one);

		public sealed override void Setup(Vector3Int size, int value = IntVoxelUtility.emptyValue)
		{
			FullSize = size;
			SetWhole(value); 
		}

		public sealed override BoundsInt VoxelBoundaries
		{
			get => new(Vector3Int.zero, size);
			protected set => Setup(value.size);
		}

		// GET Voxels ----------------------------

		public int Index(Vector3Int coordinate) => Index(coordinate.x, coordinate.y, coordinate.z);

		public int Index(int x, int y, int z) => x + (y * size.x) + (z * size.x * size.y);

		Vector3Int Index(int i)
		{
			int z = i / (size.x * size.y);
			i -= z * size.x * size.y;

			int y = i / size.x;
			i -= y * size.x;

			int x = i;

			return new Vector3Int(x, y, z);
		}

		public int GetHighestValue() => intVoxelData.Prepend(-1).Max();

		// GET Voxels ----------------------------

		public sealed override int GetVoxel(int x, int y, int z) => intVoxelData[Index(x, y, z)];

		public int GetFast(int x, int y, int z, int w, int h) => intVoxelData[x + (y * w) + (z * w * h)];

		// SET Voxels ----------------------------

		public sealed override bool SetVoxel(int x, int y, int z, int voxel)
		{
			int index = Index(x, y, z);
			if (intVoxelData[index] == voxel) return false;
			intVoxelData[index] = voxel;
			return true;
		}

		public sealed override bool SetVoxel(int x, int y, int z, VoxelAction action, int value)
		{
			int index = Index(x, y, z);
			if (index<0 || index>= intVoxelData.Length) return false;
			int v = intVoxelData[index];
			int oldVal = v;
			switch (action)
			{
				case VoxelAction.Overwrite:
					v = value;
					break;
				case VoxelAction.Repaint:
					if (oldVal != IntVoxelUtility.emptyValue)
						v = value;
					break;
				case VoxelAction.Attach:
					if (oldVal == IntVoxelUtility.emptyValue)
						v = value;
					break;
				case VoxelAction.Erase:
					v = emptyValue;
					break;
			}
			if(oldVal == v)
				return false;

			intVoxelData[index] = v;
			return true;
		}
		
		public sealed override bool SetWhole(int value)
		{
			for (int i = 0; i < intVoxelData.Length; i++)
			{
				intVoxelData[i] = value;
			}
			return true;  // Faster to say, it is always changed.
		}

		public sealed override bool SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, VoxelAction action, int value)
		{
			Vector3Int size = FullSize;
			int minX = Mathf.Max(0, Mathf.Min(startCoordinate.x, endCoordinate.x, size.x));
			int minY = Mathf.Max(0, Mathf.Min(startCoordinate.y, endCoordinate.y, size.y));
			int minZ = Mathf.Max(0, Mathf.Min(startCoordinate.z, endCoordinate.z, size.z));
			int maxX = Mathf.Min(size.x - 1, Mathf.Max(startCoordinate.x, endCoordinate.x, 0));
			int maxY = Mathf.Min(size.y - 1, Mathf.Max(startCoordinate.y, endCoordinate.y, 0));
			int maxZ = Mathf.Min(size.z - 1, Mathf.Max(startCoordinate.z, endCoordinate.z, 0));

			bool changed = false;
			if (action == VoxelAction.Overwrite)
			{
				for (int x = minX; x <= maxX; x++)
					for (int y = minY; y <= maxY; y++)
						for (int z = minZ; z <= maxZ; z++)
						{
							int index = x + (y * size.x) + (z * size.x * size.y);

							if (intVoxelData[index] != value)
							{
								intVoxelData[index] = value;
								changed |= true;
							}
						}
			}
			if (action == VoxelAction.Repaint)
			{
				for (int x = minX; x <= maxX; x++)
					for (int y = minY; y <= maxY; y++)
						for (int z = minZ; z <= maxZ; z++)
						{
							int index = x + (y * size.x) + (z * size.x * size.y);
							int originalValue = intVoxelData[index];
							if (originalValue.IsFilled() && originalValue != value)
							{
								intVoxelData[index] = value;
								changed |= true;
							}  
						}
			}
			else if (action == VoxelAction.Attach)
			{
				for (int x = minX; x <= maxX; x++)
					for (int y = minY; y <= maxY; y++)
						for (int z = minZ; z <= maxZ; z++)
						{
							int index = x + (y * size.x) + (z * size.x * size.y);
							int originalValue = intVoxelData[index];
							if (originalValue.IsEmpty() && originalValue != value)
							{
								intVoxelData[index] = value;
								changed |= true;
							}
						}
			}
			else if (action == VoxelAction.Erase)
			{
				for (int x = minX; x <= maxX; x++)
					for (int y = minY; y <= maxY; y++)
						for (int z = minZ; z <= maxZ; z++)
						{
							int index = x + (y * size.x) + (z * size.x * size.y);
							int originalValue = intVoxelData[index];
							if (originalValue != IntVoxelUtility.emptyValue)
							{
								intVoxelData[index] = IntVoxelUtility.emptyValue;
								changed |= true;
							} 
						}
			}

			return changed;
		}

	}
}