using MUtility;
using System; 
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
	public abstract partial class VoxelMap
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

		// ------------- Changed Event -------------

		public event Action MapChangedEvent;
		internal void MapChanged()
		{ 
			OnMapChanged();
			MapChangedEvent?.Invoke();
		}

		protected virtual void OnMapChanged() { }

		internal void UndoRedoEvenInvokedOnMap() =>
			MapChangedEvent?.Invoke();

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

		public abstract bool SetVoxel(int x, int y, int z, int value);
		public bool SetVoxel(Vector3Int coordinate, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, value);

		public virtual bool SetVoxel(int x, int y, int z, VoxelAction action, int value)
		{
			if (!IsValidCoord(x, y, z)) return false;
			int v = GetVoxel(x, y, z);
			int oldV = v;

			switch (action)
			{
				case VoxelAction.Override:
					v = value;
					break;
				case VoxelAction.Repaint:
					if (IsVoxelFilled(oldV))
						v = value;
					break;
				case VoxelAction.Attach:
					if (IsVoxelEmpty(oldV))
						v = value;
					break;
				case VoxelAction.Erase:
					v = emptyValue;
					break;
			}

			return SetVoxel(x, y, z, v);
		}

		public bool SetVoxel(Vector3Int coordinate, VoxelAction action, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, action, value);
		public bool FillVoxel(int x, int y, int z, int value) => SetVoxel(x, y, z, VoxelAction.Repaint, value);
		public bool FillVoxel(Vector3Int coordinate, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, VoxelAction.Attach, value);
		public bool RepaintVoxel(int x, int y, int z, int value) => SetVoxel(x, y, z, VoxelAction.Repaint, value);
		public bool RepaintVoxel(Vector3Int coordinate, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, VoxelAction.Attach, value);
		public bool ClearVoxel(Vector3Int coordinate) => ClearVoxel(coordinate.x, coordinate.y, coordinate.z);
		public bool ClearVoxel(int x, int y, int z) => SetVoxel(x, y, z, VoxelAction.Override, emptyValue);

		// ------------- Batch SET Operation -------------

		public abstract bool SetWhole(int value);

		public bool ClearWhole() => SetWhole(emptyValue);

		public abstract bool SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, VoxelAction action, int value);

		public bool SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, VoxelAction.Override, value);

		public bool FillRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, VoxelAction.Attach, value);

		public bool RepaintRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, VoxelAction.Repaint, value);

		public bool ClearRange(Vector3Int startCoordinate, Vector3Int endCoordinate) => SetRange(startCoordinate, endCoordinate, VoxelAction.Erase, emptyValue);

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
	}
}