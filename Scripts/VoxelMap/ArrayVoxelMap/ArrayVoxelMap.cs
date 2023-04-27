using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MUtility;

namespace VoxelSystem
{
	[Serializable]
	public partial class ArrayVoxelMap: VoxelMap<Voxel> 
	{
		const int defaultMapSize = 8;

		[SerializeField] int width;
		[SerializeField] int height;
		[SerializeField] int depth;
		[SerializeField] Vector3Int size;
		[SerializeField] Voxel[] voxelData;

		// Constructor -----------------------------------------------------------------------------

		public ArrayVoxelMap()
		{
			Setup(new Vector3Int(defaultMapSize, defaultMapSize, defaultMapSize));
		}

		public ArrayVoxelMap(int x, int y, int z)
		{
			Setup(new Vector3Int(x, y, z));
		}

		public ArrayVoxelMap(Vector3Int size, bool clear = false)
		{
			Setup(size, clear);
		}

		void Setup(Vector3Int size, bool clear = false)
		{
			bool isSizeInvalid = size.x <= 0 || size.y <= 0 || size.z <= 0;
			if (isSizeInvalid)
			{
				width = 0;
				height = 0;
				depth = 0;
				voxelData = null;
				return;
			}
			width = size.x;
			height = size.y;
			depth = size.z;
			voxelData = new Voxel[size.x * size.y * size.z];

			if (clear)
				for (int i = 0; i < voxelData.Length; i++)
					voxelData[i] = new Voxel(-1);
			else
				for (int i = 0; i < voxelData.Length; i++)
				{
					Vector3Int coordinate = Index(i);
					bool a = coordinate.x == 0 || coordinate.x == size.x - 1;
					bool b = coordinate.y == 0 || coordinate.y == size.y - 1;
					bool c = coordinate.z == 0 || coordinate.z == size.z - 1;
					bool filled = (a && b) || (b && c) || (c && a);
					voxelData[i] = new Voxel(filled ? 0 : -1);
				}
		}

		public override bool IsVoxelFilled(Voxel v) => v.IsFilled;

		public ArrayVoxelMap(ArrayVoxelMap original) // Copy constructor
		{
			if (original == null) return;

			width = original.width;
			height = original.height;
			depth = original.depth;
			CopyEventListeners(original);

			if (original.voxelData != null)
				voxelData = MakeCopy(original.voxelData);

			static T[] MakeCopy<T>(IReadOnlyList<T> source)
			{
				var copy = new T[source.Count];
				for (int i = 0; i < source.Count; i++)
					copy[i] = source[i];
				return copy;
			}
		}

		internal override VoxelMap GetCopy() => new ArrayVoxelMap(this);

		public override IntBounds VoxelBoundaries => new (Vector3Int.zero, new Vector3Int(width, height, depth));

		// GET Voxels ----------------------------

		public int Index(Vector3Int coordinate) => Index(coordinate.x, coordinate.y, coordinate.z);

		public int Index(int x, int y, int z) => x + (y * width) + (z * width * height);

		Vector3Int Index(int i)
		{
			int z = i / (width * height);
			i -= z * width * height;

			int y = i / width;
			i -= y * width;

			int x = i;

			return new Vector3Int(x, y, z);
		}

		public int GetHighestValue() => voxelData.Select(v => v.value).Prepend(-1).Max();

		// GET Voxels ----------------------------

		public override Voxel GetVoxel(int x, int y, int z)
		{
			size = new Vector3Int(width, height, depth);
			return voxelData[Index(x, y, z)];
		}

		public Voxel GetFast(int x, int y, int z, int w, int h) => voxelData[x + (y * w) + (z * w * h)];

		// SET Voxels ----------------------------

		public bool Set(int x, int y, int z, SetAction action, int value)
		{
			if (!IsValidCoord(x, y, z)) return false; 
			Voxel v = voxelData[Index(x, y, z)];
			Voxel oldV = v;

			if (action == SetAction.Repaint)
			{
				if (oldV.IsFilled)
				{ v.value = value; }
			}
			else if (action == SetAction.Fill)
			{
				if (v.IsEmpty)
				{ v.value = value; }
			}
			else if (action == SetAction.Clear)
			{
				v.Clear();
			}

			voxelData[Index(x, y, z)] = v;

			return !oldV.Equals(v);
		}

		public bool Set(Vector3Int coordinate, SetAction action, int value) => Set(coordinate.x, coordinate.y, coordinate.z, action, value);

		public bool Set(Vector3Int coordinate, int value) => Set(coordinate.x, coordinate.y, coordinate.z, SetAction.Fill, value);

		public bool Set(int x, int y, int z, int value) => Set(x, y, z, SetAction.Fill, value);

		public bool SetValueOf(Vector3Int coordinate, int value) => Set(coordinate.x, coordinate.y, coordinate.z, SetAction.Repaint, value);

		public bool SetValueOf(int x, int y, int z, int value) => Set(x, y, z, SetAction.Repaint, value);

		public bool ClearVoxel(Vector3Int coordinate) => ClearVoxel(coordinate.x, coordinate.y, coordinate.z);

		public bool ClearVoxel(int x, int y, int z) => Set(x, y, z, SetAction.Clear, value: 0);

		public void ClearWhole()
		{
			for (int i = 0; i < voxelData.Length; i++)
			{
				voxelData[i] = new Voxel(value: -1);
			}
			MapChanged();
		}

		public void FillWhole(int value)
		{
			for (int i = 0; i < voxelData.Length; i++)
			{
				voxelData[i] = new Voxel(value);
			}
			MapChanged();
		}

		public void FillRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, SetAction.Fill, value);

		public void ClearRange(Vector3Int startCoordinate, Vector3Int endCoordinate) => SetRange(startCoordinate, endCoordinate, SetAction.Clear, value: 0);

		public void SetValueOfRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, SetAction.Repaint, value);

		public void SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, SetAction action, int value)
		{
			// This code need to be highly optimized;
			// this is why its not clean

			Vector3Int size = Size;
			int minX = Mathf.Max(a: 0, Mathf.Min(startCoordinate.x, endCoordinate.x, size.x));
			int minY = Mathf.Max(a: 0, Mathf.Min(startCoordinate.y, endCoordinate.y, size.y));
			int minZ = Mathf.Max(a: 0, Mathf.Min(startCoordinate.z, endCoordinate.z, size.z));
			int maxX = Mathf.Min(size.x - 1, Mathf.Max(startCoordinate.x, endCoordinate.x, 0));
			int maxY = Mathf.Min(size.y - 1, Mathf.Max(startCoordinate.y, endCoordinate.y, 0));
			int maxZ = Mathf.Min(size.z - 1, Mathf.Max(startCoordinate.z, endCoordinate.z, 0));

			if (action == SetAction.Repaint)
			{
				for (int x = minX; x <= maxX; x++)
				{
					for (int y = minY; y <= maxY; y++)
					{
						for (int z = minZ; z <= maxZ; z++)
						{
							int index = x + (y * size.x) + (z * size.x * size.y);
							if (voxelData[index].IsFilled)
							{ voxelData[index].value = value; }
						}
					}
				}
			}
			else if (action == SetAction.Fill)
			{
				for (int x = minX; x <= maxX; x++)
				{
					for (int y = minY; y <= maxY; y++)
					{
						for (int z = minZ; z <= maxZ; z++)
						{
							int index = x + (y * size.x) + (z * size.x * size.y);
							if (voxelData[index].IsEmpty)
							{ voxelData[index].value = value; }
						}
					}
				}
			}
			else if (action == SetAction.Clear)
			{
				for (int x = minX; x <= maxX; x++)
				{
					for (int y = minY; y <= maxY; y++)
					{
						for (int z = minZ; z <= maxZ; z++)
						{
							int index = x + (y * size.x) + (z * size.x * size.y);
							if (voxelData[index].IsFilled)
							{ voxelData[index].value = -1; }
						}
					}
				}
			}

			MapChanged();
		}

		public void CopyFromOtherMap(ArrayVoxelMap sourceMap, Vector3Int startCoordinateOfSourceMap, Vector3Int startCoordinateOfDestinationMap, Vector3Int copySize)
		{
			bool mapChanged = false;

			for (int x = 0; x < copySize.x; x++)
			{
				int destinationX = startCoordinateOfDestinationMap.x + x;
				int sourceX = startCoordinateOfSourceMap.x + x;
				if (destinationX >= width || destinationX < 0)
				{ continue; }
				if (sourceX >= sourceMap.width || sourceX < 0)
				{ continue; }

				for (int y = 0; y < copySize.y; y++)
				{
					int destinationY = startCoordinateOfDestinationMap.y + y;
					int sourceY = startCoordinateOfSourceMap.y + y;
					if (destinationY >= height || destinationY < 0)
					{ continue; }
					if (sourceY >= sourceMap.height || sourceY < 0)
					{ continue; }

					for (int z = 0; z < copySize.z; z++)
					{
						int destinationZ = startCoordinateOfDestinationMap.z + z;
						int sourceZ = startCoordinateOfSourceMap.z + z;
						if (destinationZ >= depth || destinationZ < 0)
						{ continue; }
						if (sourceZ >= sourceMap.depth || sourceZ < 0)
						{ continue; }

						// COPY VOXEL

						int val = sourceMap.GetVoxel(sourceX, sourceY, sourceZ).value;
						if (val >= 0)
						{
							mapChanged |= Set(destinationX, destinationY, destinationZ, val);
						}
					}
				}
			}
			if (mapChanged)
				MapChanged();
		}

		// Voxel Map Info --------------------------------------------

		public Vector3Int Size => new(width, height, depth);

		public int GetSize(Axis3D a)
		{
			if (a == Axis3D.X)
				return width;
			if (a == Axis3D.Y)
				return height;
			return depth;
		}

		public int Width => width;
		public int Height => height;
		public int Depth => depth;
	}
}