using MUtility;
using System;
using System.Drawing;
using UnityEngine;

namespace VoxelSystem
{
	public struct VoxelHit
	{
		public Vector3Int voxelIndex;
		public GeneralDirection3D side;
		public Vector3 hitWorldPosition;  // Is it wordPos?
	}


	[Serializable]
	public abstract class VoxelMap
	{
		// ------------- GET Information -------------

		protected const int emptyValue = IntVoxelUtility.emptyValue;
		public bool IsVoxelFilled(int v) => v != emptyValue;
		public bool IsVoxelEmpty(int v) => v == emptyValue;

		public abstract IntBounds VoxelBoundaries { get; protected set; }
		public abstract Vector3Int FullSize { get; protected set; }

		public bool IsValidCoord(int x, int y, int z) => VoxelBoundaries.Contains(x, y, z);

		public bool IsValidCoord(Vector3Int coordinate) => IsValidCoord(coordinate.x, coordinate.y, coordinate.z);

		public int GetSize(Axis3D a)
		{
			if (a == Axis3D.X)
				return FullSize.x;
			if (a == Axis3D.Y)
				return FullSize.y;
			return FullSize.z;
		}
		// ------------- Constructing -------------

		public abstract void Setup();
		public abstract void Setup(Vector3Int size, int value = emptyValue);
		public void SetupFrom(VoxelMap map)
		{ 
			FullSize = map.FullSize;
			foreach (Vector3Int index in map.VoxelBoundaries.WalkThrough())
			{ 
				SetVoxel(index, map.GetVoxel(index));
			}
			MapChanged();
		}

		// ------------- Changed Event -------------
		public event Action MapChangedEvent;
		protected void MapChanged()
		{
			OnMapChanged();
			MapChangedEvent?.Invoke();
		}

		protected virtual void OnMapChanged() { }

		internal void UndoRedoEvenInvokedOnMap() =>
			MapChangedEvent?.Invoke();

		protected void CopyEventListeners(VoxelMap original) =>
			MapChangedEvent = original.MapChangedEvent;

		// ------------- GET Voxels -------------

		public abstract int GetVoxel(int x, int y, int z);
		public int GetVoxel(Vector3Int coordinate) => GetVoxel(coordinate.x, coordinate.y, coordinate.z);

		public bool TryGetVoxel(Vector3Int index, out int voxel)
		{
			if (!IsValidCoord(index))
			{
				voxel = default;
				return false;
			}
			voxel = GetVoxel(index.x, index.y, index.z);
			return true;
		}

		public bool IsFilledSafe(Vector3Int coordinate) =>
			TryGetVoxel(coordinate, out int voxel) && IsVoxelFilled(voxel);

		// ------------- SET Voxels -------------
		public enum SetAction
		{
			Set,      // Set all voxels to fix value
			Fill,     // Set all EMPTY voxels to fix Value
			Repaint,  // Set all NON-EMPTY voxels to fix Value
			Clear     // Set all voxels to EMPTY
		}
		public abstract bool SetVoxel(int x, int y, int z, int value);
		public bool SetVoxel(Vector3Int coordinate, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, value);

		public virtual bool SetVoxel(int x, int y, int z, SetAction action, int value)
		{
			if (!IsValidCoord(x, y, z)) return false;
			int v = GetVoxel(x, y, z);
			int oldV = v;

			switch (action)
			{
				case SetAction.Set:
					v = value;
					break;
				case SetAction.Repaint:
					if (IsVoxelFilled(oldV))
						v = value;
					break;
				case SetAction.Fill:
					if (IsVoxelEmpty(oldV))
						v = value;
					break;
				case SetAction.Clear:
					v = emptyValue;
					break;
			}

			return SetVoxel(x, y, z, v);
		}

		public bool SetVoxel(Vector3Int coordinate, SetAction action, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, action, value);
		public bool FillVoxel(int x, int y, int z, int value) => SetVoxel(x, y, z, SetAction.Repaint, value);
		public bool FillVoxel(Vector3Int coordinate, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, SetAction.Fill, value);
		public bool RepaintVoxel(int x, int y, int z, int value) => SetVoxel(x, y, z, SetAction.Repaint, value);
		public bool RepaintVoxel(Vector3Int coordinate, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, SetAction.Fill, value);
		public bool ClearVoxel(Vector3Int coordinate) => ClearVoxel(coordinate.x, coordinate.y, coordinate.z);
		public bool ClearVoxel(int x, int y, int z) => SetVoxel(x, y, z, SetAction.Set, emptyValue);

		public void CopyFrom(VoxelMap sourceMap, Vector3Int startCoordinateOfSourceMap, Vector3Int startCoordinateOfDestinationMap, Vector3Int copySize)
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

						// Copy Voxel
						int val = sourceMap.GetVoxel(sourceX, sourceY, sourceZ);
						if (val != IntVoxelUtility.emptyValue)
						{
							mapChanged |= SetVoxel(destinationX, destinationY, destinationZ, val);
						}
					}
				}
			}
			if (mapChanged)
				MapChanged();
		}

		// ------------- Batch SET Operation -------------

		public abstract void SetWhole(int value);

		public void ClearWhole() => SetWhole(emptyValue);

		public abstract void SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, SetAction action, int value);

		public void SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, SetAction.Set, value);

		public void FillRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, SetAction.Fill, value);

		public void RepaintRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, SetAction.Repaint, value);

		public void ClearRange(Vector3Int startCoordinate, Vector3Int endCoordinate) => SetRange(startCoordinate, endCoordinate, SetAction.Clear, emptyValue);

		// ------------- RayCast -------------

		public bool Raycast(Ray globalRay, out VoxelHit hit, Transform voxelTransform, bool returnOutsideVoxel = false)
		{
			if (voxelTransform == null)
			{
				hit = new VoxelHit();
				return false;
			}
			Matrix4x4 matrix = voxelTransform.worldToLocalMatrix;
			Ray localRay = globalRay.Transform(matrix);
			return Raycast(localRay, out hit, returnOutsideVoxel);
		}

		public bool Raycast(Ray globalRay, out VoxelHit hit, Matrix4x4 matrix, bool returnOutsideVoxel = false) =>
			Raycast(globalRay.Transform(matrix), out hit, returnOutsideVoxel);

		protected abstract bool Raycast(Ray localRay, out VoxelHit hit, bool returnOutsideVoxel = false);

		// ------------- Transform Operations -------------
		public enum ResizeType { Resize, Repeat, Rescale }

		public abstract void Turn(Axis3D axis, bool leftHandPositive);
		public abstract void Mirror(Axis3D axis);
		public abstract void Resize(GeneralDirection3D direction, int steps, ResizeType type);

	}

	[Serializable]
	public abstract class VoxelMap<TMap> : VoxelMap where TMap : VoxelMap
	{		
		internal abstract TMap GetCopy();

	}
}