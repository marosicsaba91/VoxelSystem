using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoxelSystem
{
	public enum MapChange { None, Quick, Final }
	public struct VoxelHit
	{
		public Vector3Int voxelIndex;
		public GeneralDirection3D side;
		public Vector3 hitWorldPosition;  // Is it wordPos?
	}

	public delegate void MapChangedDelegate(bool isQuick);

	[Serializable]
	public abstract partial class VoxelMap
	{
		static readonly Dictionary<string, VoxelMap> mapDictionary = new();
		public static bool TryGetMapByGuid(string guid, out VoxelMap map)
		{
			map = null;
			foreach (KeyValuePair<string, VoxelMap> item in mapDictionary)
			{
				if (item.Key == guid)
				{
					map = item.Value;
					return true;
				}
			}

			VoxelFilter[] filters = Object.FindObjectsByType<VoxelFilter>(FindObjectsSortMode.None);
			foreach (VoxelFilter filter in filters)
			{
				VoxelMap vMap = filter.GetVoxelMap();
				mapDictionary.TryAdd(vMap.UniqueID, vMap);
				if (vMap.UniqueID.Equals(guid))
					map = vMap;
			}

			return map != null;
		}


		// ------------- GET Information -------------

		public bool IsVoxelEmpty(int voxelValue) => voxelValue.IsEmpty();

		public abstract BoundsInt VoxelBoundaries { get; protected set; }
		public abstract Vector3Int FullSize { get; protected set; }

		public virtual int Length
		{
			get
			{
				Vector3Int size = FullSize;
				return size.x * size.y * size.z;
			}
		}

		[SerializeField] string uniqueID;
		public string UniqueID => uniqueID;

		public bool IsValidCoord(int x, int y, int z) => VoxelBoundaries.Contains(new Vector3Int(x, y, z));

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

		protected void SetupUniqueID()
		{
			uniqueID = Guid.NewGuid().ToString();
			mapDictionary.TryAdd(UniqueID, this);
		}

		public abstract void Setup();

		protected const int emptyValue = IntVoxelUtility.emptyValue;
		public abstract void Setup(Vector3Int size, int value = emptyValue);

		// ------------- Changed Event -------------


		public event MapChangedDelegate MapChangedEvent;

		internal void MapChanged(MapChange change)
		{
			if (change == MapChange.None) return;
			bool isQuick = change == MapChange.Quick;
			OnMapChanged(isQuick);
			MapChangedEvent?.Invoke(isQuick);
		}

		protected virtual void OnMapChanged(bool isQuick) { }

		internal void UndoRedoEvenInvokedOnMap() =>
			MapChangedEvent?.Invoke(false);

		// ------------- GET Voxels -------------

		public abstract int GetVoxel(int x, int y, int z);
		public int GetVoxel(Vector3Int coordinate) => GetVoxel(coordinate.x, coordinate.y, coordinate.z);

		public bool TryGetVoxel(Vector3Int index, out int voxel) =>
			TryGetVoxel(index.x, index.y, index.z, out voxel);
		public bool TryGetVoxel(int x, int y, int z, out int voxel)
		{
			if (!IsValidCoord(x, y, z))
			{
				voxel = default;
				return false;
			}
			voxel = GetVoxel(x, y, z);
			return true;
		}

		public bool IsFilledSafe(Vector3Int coordinate) =>
			TryGetVoxel(coordinate, out int voxel) && voxel.IsFilled();

		// ------------- SET Voxels -------------

		public abstract bool SetVoxel(int x, int y, int z, int value);
		public bool SetVoxel(Vector3Int coordinate, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, value);
		public bool SetVoxel(Vector3Int coordinate, VoxelAction action, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, action, value);

		public virtual bool SetVoxel(int x, int y, int z, VoxelAction action, int value)
		{
			if (!IsValidCoord(x, y, z)) return false;
			int v = GetVoxel(x, y, z);
			int oldV = v;

			switch (action)
			{
				case VoxelAction.Overwrite:
					v = value;
					break;
				case VoxelAction.Repaint:
					if (oldV.IsFilled())
						v = value;
					break;
				case VoxelAction.Attach:
					if (oldV.IsEmpty())
						v = value;
					break;
				case VoxelAction.Erase:
					v.SetEmpty();
					break;
			}

			return SetVoxel(x, y, z, v);
		}

		public bool FillVoxel(int x, int y, int z, int value) => SetVoxel(x, y, z, VoxelAction.Repaint, value);
		public bool FillVoxel(Vector3Int coordinate, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, VoxelAction.Attach, value);
		public bool RepaintVoxel(int x, int y, int z, int value) => SetVoxel(x, y, z, VoxelAction.Repaint, value);
		public bool RepaintVoxel(Vector3Int coordinate, int value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, VoxelAction.Attach, value);
		public bool ClearVoxel(Vector3Int coordinate) => ClearVoxel(coordinate.x, coordinate.y, coordinate.z);
		public bool ClearVoxel(int x, int y, int z) => SetVoxel(x, y, z, VoxelAction.Overwrite, emptyValue);

		// ------------- Batch SET Operation -------------

		public abstract bool SetWhole(int value);

		public bool ClearWhole() => SetWhole(emptyValue);

		public abstract bool SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, VoxelAction action, int value);

		public bool SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, VoxelAction.Overwrite, value);

		public bool FillRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, VoxelAction.Attach, value);

		public bool RepaintRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, VoxelAction.Repaint, value);

		public bool ClearRange(Vector3Int startCoordinate, Vector3Int endCoordinate) => SetRange(startCoordinate, endCoordinate, VoxelAction.Erase, emptyValue);

		internal bool SetRange(BoundsInt bound, int paletteIndex)
		{
			bool changed = false;
			foreach (Vector3Int c in bound.WalkThrough())
				changed |= SetVoxel(c, paletteIndex);
			return changed;
		}

		internal bool ClearRange(BoundsInt bound) => SetRange(bound, emptyValue);

		public bool ResetRange(VoxelMap original, Vector3Int startCoordinate, Vector3Int endCoordinate)
		{
			bool changed = false;
			Vector3Int min = Vector3Int.Min(startCoordinate, endCoordinate);
			min = Vector3Int.Max(min, Vector3Int.zero);
			Vector3Int max = Vector3Int.Max(startCoordinate, endCoordinate);
			max = Vector3Int.Min(max, FullSize - Vector3Int.one);

			BoundsInt bound = new(min, max + Vector3Int.one - min);
			foreach (Vector3Int index in bound.WalkThrough())
			{
				int v = original.GetVoxel(index);
				changed |= SetVoxel(index, v);
			}
			return changed;
		}


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