using MUtility;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

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
		static Dictionary<string, VoxelMap> mapDictionary = new();
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

			VoxelFilter[] filters = Object.FindObjectsByType<VoxelFilter>( FindObjectsSortMode.None );
			foreach (VoxelFilter filter in filters)
			{
				VoxelMap vMap = filter.GetVoxelMap(); 
				mapDictionary.TryAdd(vMap.UniqueID, vMap);
				if(vMap.UniqueID.Equals(guid))
					map = vMap;
			}

			return map != null;
		}


		// ------------- GET Information -------------

		protected const int emptyValue = IntVoxelUtility.emptyValue;
		public bool IsVoxelFilled(int v) => v != emptyValue;
		public bool IsVoxelEmpty(int v) => v == emptyValue;

		public abstract BoundsInt VoxelBoundaries { get; protected set; }
		public abstract Vector3Int FullSize { get; protected set; }
		[SerializeField] string uniqueID;
		public string UniqueID => uniqueID; 

		public bool IsValidCoord(int x, int y, int z) => VoxelBoundaries.Contains(new Vector3Int(x,y,z));

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
				case VoxelAction.Overwrite:
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

		// ------------- FloodFill -------------

		static HashSet<Vector3Int> _floodFillAlreadyChecked = new HashSet<Vector3Int>();
		static HashSet<Vector3Int> _nextRound1 = new HashSet<Vector3Int>();
		static HashSet<Vector3Int> _nextRound2 = new HashSet<Vector3Int>();
		int _floodFillRoundIndex = 0;

		public bool FloodFill(Vector3Int index, int value, VoxelAction action, bool everyFilled) 
		{
			BenchmarkTimer bt = new BenchmarkTimer();
			bt.StartModule("FF");
			int voxel = GetVoxel(index);
			if (action == VoxelAction.Erase)
				value = emptyValue;
			if (value == emptyValue && voxel == emptyValue) return false;
			if (!everyFilled && voxel == value) return false;

			_floodFillAlreadyChecked.Clear();
			_nextRound1.Clear();
			_nextRound2.Clear();
			_nextRound1.Add(index);
			_floodFillRoundIndex = 0;

			FloodFill(voxel, value, everyFilled);

			Debug.Log(bt);
			return true;
		}

		void FloodFill(int startValue, int changeValue, bool everyFilled)
		{
			HashSet<Vector3Int> current, next;
			do
			{
				current = _floodFillRoundIndex % 2 == 0 ? _nextRound1 : _nextRound2;
				next = _floodFillRoundIndex % 2 == 0 ? _nextRound2 : _nextRound1;

				next.Clear();
				GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;

				foreach (Vector3Int index in current)
				{
					SetVoxel(index, changeValue);
					_floodFillAlreadyChecked.Add(index);


					for (int i = 0; i < directions.Length; i++)
					{
						GeneralDirection3D direction = directions[i];
						Vector3Int nextIndex = direction.ToVectorInt() + index;
						if (_floodFillAlreadyChecked.Contains(nextIndex))
							continue;
						if (!IsValidCoord(nextIndex))
						{
							_floodFillAlreadyChecked.Add(nextIndex);
							continue;
						}
						int nextVoxel = GetVoxel(nextIndex);

						bool isDifferent = everyFilled 
							? nextVoxel.IsFilled() != startValue.IsFilled() 
							: nextVoxel != startValue; 

						if (isDifferent)
						{
							_floodFillAlreadyChecked.Add(nextIndex);
							continue;
						}


						next.Add(nextIndex);
					}
				}

				_floodFillRoundIndex++;
			} while (!next.IsEmpty());
		} 
	}
}