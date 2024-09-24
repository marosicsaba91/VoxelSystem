using System;
using UnityEngine;

namespace VoxelSystem
{

	[Serializable]
	public partial class ArrayVoxelMap : VoxelMap, ISerializationCallbackReceiver
	{
		public const int defaultMapSize = 8;

		[SerializeField] Vector3Int size;
		[SerializeField] long[] longVoxelData = new long[0];
		Voxel[] voxelData = new Voxel[0];

		public void OnBeforeSerialize()
		{
			longVoxelData = new long[voxelData.Length];
			for (int i = 0; i < voxelData.Length; i++)
			{
				longVoxelData[i] = voxelData[i].ToLong();
			}
		}

		public void OnAfterDeserialize()
		{
			if (longVoxelData == null) return;
			voxelData = new Voxel[longVoxelData.Length];
			for (int i = 0; i < voxelData.Length; i++)
			{
				voxelData[i] = new Voxel(longVoxelData[i]);
			}
		}

		public sealed override Vector3Int FullSize
		{
			get => size;
			protected set // Reset Whole Map
			{
				bool isSizeInvalid = value.x <= 0 || value.y <= 0 || value.z <= 0;
				if (isSizeInvalid)
					throw new ArgumentException("Size must be positive");
				if (voxelData != null && size == value) return;

				size = value;
				voxelData = new Voxel[Length];
			}
		}

		public sealed override int Length => size.x * size.y * size.z;

		// Constructor -----------------------------------------------------------------------------     

		public ArrayVoxelMap() => Setup(Vector3Int.one * 10, emptyValue);

		public ArrayVoxelMap(Vector3Int size) => Setup(size, emptyValue);
		public ArrayVoxelMap(Vector3Int size, Voxel value)
		{
			SetupUniqueID();
			Setup(size, value);
		}

		public sealed override void Setup(Vector3Int size, Voxel value)
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

		public int Index(Vector3Int coordinate) => Index(coordinate.x, coordinate.y, coordinate.z, size);
		public static int GetIndex(Vector3Int coordinate, Vector3Int size) =>
			Index(coordinate.x, coordinate.y, coordinate.z, size);

		public int Index(int x, int y, int z) => Index(x, y, z, size);
		public static int Index(int x, int y, int z, Vector3Int size) =>
			x + (y * size.x) + (z * size.x * size.y);

		Vector3Int GetCoordinate(int i) => GetCoordinate(i, size);
		public static Vector3Int GetCoordinate(int i, Vector3Int size)
		{
			int z = i / (size.x * size.y);
			i -= z * size.x * size.y;

			int y = i / size.x;
			i -= y * size.x;

			int x = i;

			return new Vector3Int(x, y, z);
		}

		// public Voxel GetHighestValue() => voxelData.Prepend(-1).Max();

		// GET Voxels ----------------------------

		public sealed override Voxel GetVoxel(int x, int y, int z) => voxelData[Index(x, y, z)];

		// SET Voxels ----------------------------

		public sealed override bool SetVoxel(int x, int y, int z, Voxel voxel)
		{
			int index = Index(x, y, z);
			if (voxelData[index] == voxel) return false;
			voxelData[index] = voxel;
			return true;
		}

		public sealed override bool SetVoxel(int x, int y, int z, VoxelAction action, Voxel value)
		{
			int index = Index(x, y, z);
			if (index < 0 || index >= voxelData.Length) return false;
			Voxel v = voxelData[index];
			Voxel oldVal = v;
			switch (action)
			{
				case VoxelAction.Overwrite:
					v = value;
					break;
				case VoxelAction.Repaint:
					if (oldVal.IsFilled())
						v = value;
					break;
				case VoxelAction.Attach:
					if (oldVal.IsEmpty())
						v = value;
					break;
				case VoxelAction.Erase:
					v = emptyValue;
					break;
				case VoxelAction.RepaintMaterialOnly:
					if (v.IsFilled())
						v.materialIndex = value.materialIndex;
					break;
				case VoxelAction.RepaintShapeOnly:
					if (v.IsFilled())
					{
						v.shapeId = value.shapeId;
						v.cubicTransformationIndex = value.cubicTransformationIndex;
						v.extraData = value.extraData;
					}
					break;

			}
			if (oldVal == v)
				return false;

			voxelData[index] = v;
			return true;
		}

		public sealed override bool SetWhole(Voxel value)
		{
			for (int i = 0; i < voxelData.Length; i++)
			{
				voxelData[i] = value;
			}
			return true;  // Faster to say, it is always changed.
		}

		public sealed override bool ClearWhole()
		{
			voxelData = new Voxel[Length];
			Array.Fill(voxelData, emptyValue);

			return true;
		}

		public sealed override bool SetRange
			(Vector3Int startCoordinate, Vector3Int endCoordinate, VoxelAction action, Voxel value)
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

							if (voxelData[index] != value)
							{
								voxelData[index] = value;
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
							Voxel originalValue = voxelData[index];
							if (originalValue.IsFilled() && originalValue != value)
							{
								voxelData[index] = value;
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
							Voxel originalValue = voxelData[index];
							if (originalValue.IsEmpty() && originalValue != value)
							{
								voxelData[index] = value;
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
							Voxel originalValue = voxelData[index];
							if (originalValue.IsFilled())
							{
								voxelData[index] = emptyValue;
								changed |= true;
							}
						}
			}
			else if (action == VoxelAction.RepaintMaterialOnly)
			{
				for (int x = minX; x <= maxX; x++)
					for (int y = minY; y <= maxY; y++)
						for (int z = minZ; z <= maxZ; z++)
						{
							int index = x + (y * size.x) + (z * size.x * size.y);
							Voxel originalValue = voxelData[index];
							if (originalValue.IsFilled())
							{
								voxelData[index].materialIndex = value.materialIndex;
								changed |= true;
							}
						}
			}
			else if (action == VoxelAction.RepaintShapeOnly)
			{
				for (int x = minX; x <= maxX; x++)
					for (int y = minY; y <= maxY; y++)
						for (int z = minZ; z <= maxZ; z++)
						{
							int index = x + (y * size.x) + (z * size.x * size.y);
							Voxel originalValue = voxelData[index];
							if (originalValue.IsFilled())
							{
								Voxel v = voxelData[index];
								v.shapeId = value.shapeId;
								v.extraData = value.extraData;
								voxelData[index] = v;
								changed |= true;
							}
						}
			}

			return changed;
		}

		static ArrayVoxelMap oneVoxelMap;

		internal static ArrayVoxelMap GetTestOneVoxelMap(Voxel value)
		{
			if (oneVoxelMap == null)
			{
				oneVoxelMap = new ArrayVoxelMap();
				oneVoxelMap.Setup(Vector3Int.one * 3);
			}
			oneVoxelMap.SetVoxel(Vector3Int.one, value);
			Voxel v = oneVoxelMap.GetVoxel(Vector3Int.one);

			return oneVoxelMap;
		}
	}
}