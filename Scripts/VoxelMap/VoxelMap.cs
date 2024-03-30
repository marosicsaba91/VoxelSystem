using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoxelSystem
{
	public enum MapChange { None, Edit, Final }

	[Serializable]
	public struct VoxelHit
	{
		public Vector3Int voxelIndex;
		public GeneralDirection3D side;
		public Vector3 hitWorldPosition;  // Is it wordPos?
	}

	public delegate void MapChangedDelegate(bool isFinal);

	[Serializable]
	public abstract partial class VoxelMap
	{
		// public abstract void GetSize();

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

			VoxelObject[] filters = Object.FindObjectsByType<VoxelObject>(FindObjectsSortMode.None);
			foreach (VoxelObject filter in filters)
			{
				VoxelMap vMap = filter.GetVoxelMap();
				mapDictionary.TryAdd(vMap.UniqueID, vMap);
				if (vMap.UniqueID.Equals(guid))
					map = vMap;
			}

			return map != null;
		}


		// ------------- GET Information -------------

		public bool IsVoxelEmpty(Voxel voxelValue) => voxelValue.IsEmpty();

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

		public Vector3Int ClampCoordinate(Vector3Int c)
		{
			Vector3Int size = FullSize;
			c.x = Mathf.Clamp(c.x, 0, size.x - 1);
			c.y = Mathf.Clamp(c.y, 0, size.y - 1);
			c.z = Mathf.Clamp(c.z, 0, size.z - 1);
			return c;
		}

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

		public void Setup() => Setup(FullSize, emptyValue);

		protected readonly Voxel emptyValue = Voxel.emptyValue;
		public void Setup(Vector3Int size) => Setup(size, emptyValue);
		public abstract void Setup(Vector3Int size, Voxel value);

		// ------------- Changed Event -------------


		public event MapChangedDelegate MapChangedEvent;

		internal void MapChanged(MapChange change)
		{
			if (change == MapChange.None) return;
			bool isFinal = change == MapChange.Final;
			OnMapChanged(isFinal);
			MapChangedEvent?.Invoke(isFinal);
		}

		protected virtual void OnMapChanged(bool isFinal) { }

		internal void UndoRedoEvenInvokedOnMap()
		{
			MapChangedEvent?.Invoke(isFinal: true);
		}

		// ------------- GET Voxels -------------

		public abstract Voxel GetVoxel(int x, int y, int z);
		public Voxel GetVoxel(Vector3Int coordinate) => GetVoxel(coordinate.x, coordinate.y, coordinate.z);

		public bool TryGetVoxel(Vector3Int index, out Voxel voxel) =>
			TryGetVoxel(index.x, index.y, index.z, out voxel);
		public bool TryGetVoxel(int x, int y, int z, out Voxel voxel)
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
			TryGetVoxel(coordinate, out Voxel voxel) && voxel.IsFilled();

		public bool IsFilledSafe(Vector3Int coordinate, GeneralDirection3D side) =>
			TryGetVoxel(coordinate, out Voxel voxel) && voxel.IsFilled() && voxel.IsSideClosed(side);

		// ------------- SET Voxels -------------

		public abstract bool SetVoxel(int x, int y, int z, Voxel value);
		public bool SetVoxel(Vector3Int coordinate, Voxel value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, value);
		public bool SetVoxel(Vector3Int coordinate, VoxelAction action, Voxel value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, action, value);

		public virtual bool SetVoxel(int x, int y, int z, VoxelAction action, Voxel value)
		{
			if (!IsValidCoord(x, y, z)) return false;
			Voxel v = GetVoxel(x, y, z);
			Voxel oldV = v;

			switch (action)
			{
				case VoxelAction.Overwrite:
					v = value;
					break;
				case VoxelAction.Attach:
					if (oldV.IsEmpty())
						v = value;
					break;
				case VoxelAction.Erase:
					v = emptyValue;
					break;
				case VoxelAction.Repaint:
					if (oldV.IsFilled())
						v = value;
					break;
				case VoxelAction.RepaintMaterialOnly:
					if (oldV.IsFilled())
						v.materialIndex =value.materialIndex;
					break;
				case VoxelAction.RepaintShapeOnly:
					if (oldV.IsFilled())
					{
						v.shapeId = value.shapeId;
						v.cubicTransformationIndex = value.cubicTransformationIndex;
						v.extraData = value.extraData;
					}
					break;
			}

			return SetVoxel(x, y, z, v);
		}

		public bool FillVoxel(int x, int y, int z, Voxel value) => SetVoxel(x, y, z, VoxelAction.Repaint, value);
		public bool FillVoxel(Vector3Int coordinate, Voxel value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, VoxelAction.Attach, value);
		public bool RepaintVoxel(int x, int y, int z, Voxel value) => SetVoxel(x, y, z, VoxelAction.Repaint, value);
		public bool RepaintVoxel(Vector3Int coordinate, Voxel value) => SetVoxel(coordinate.x, coordinate.y, coordinate.z, VoxelAction.Attach, value);
		public bool ClearVoxel(Vector3Int coordinate) => ClearVoxel(coordinate.x, coordinate.y, coordinate.z);
		public bool ClearVoxel(int x, int y, int z) => SetVoxel(x, y, z, VoxelAction.Overwrite, emptyValue);

		// ------------- Batch SET Operation -------------

		public abstract bool SetWhole(Voxel value);

		public abstract bool ClearWhole();

		public abstract bool SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, VoxelAction action, Voxel value);

		public bool SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, Voxel value) => SetRange(startCoordinate, endCoordinate, VoxelAction.Overwrite, value);

		public bool FillRange(Vector3Int startCoordinate, Vector3Int endCoordinate, Voxel value) => SetRange(startCoordinate, endCoordinate, VoxelAction.Attach, value);

		public bool RepaintRange(Vector3Int startCoordinate, Vector3Int endCoordinate, Voxel value) => SetRange(startCoordinate, endCoordinate, VoxelAction.Repaint, value);

		public bool ClearRange(Vector3Int startCoordinate, Vector3Int endCoordinate) => SetRange(startCoordinate, endCoordinate, VoxelAction.Erase, emptyValue);

		internal bool SetRange(BoundsInt bound, Voxel value)
		{
			bool changed = false;
			foreach (Vector3Int c in bound.WalkThrough())
				changed |= SetVoxel(c, value);
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
				Voxel v = original.GetVoxel(index);
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