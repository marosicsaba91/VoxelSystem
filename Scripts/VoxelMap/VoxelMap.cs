using System;
using System.Reflection;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public struct IntBounds
	{
		public Vector3Int min;
		public Vector3Int maxExclusive;
		public Vector3Int Size => maxExclusive - min;
		public Vector3 Center => (Vector3)(min + maxExclusive) / 2f;

		public IntBounds(Vector3Int min, Vector3Int maxExclusive)
		{
			this.min = min;
			this.maxExclusive = maxExclusive;
		}
	}

	[Serializable]
	public abstract class VoxelMap
	{
		public abstract IntBounds VoxelBoundaries { get; }


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

		// ------------- Copy Map -------------
		internal abstract VoxelMap GetCopy();

		// ------------- GET Information -------------

		public bool IsValidCoord(int x, int y, int z)
		{
			IntBounds boundaries = VoxelBoundaries;
			return
				x >= boundaries.min.x && x < boundaries.maxExclusive.x &&
				y >= boundaries.min.y && y < boundaries.maxExclusive.y &&
				z >= boundaries.min.z && z < boundaries.maxExclusive.z;
		}

		public bool IsValidCoord(Vector3Int coordinate) => IsValidCoord(coordinate.x, coordinate.y, coordinate.z);

		// ------------- SET Voxels -------------
		public enum SetAction
		{
			Set,      // Set all voxels to fix value
			Fill,     // Set all EMPTY voxels to fix Value
			Repaint,  // Set all NON-EMPTY voxels to fix Value
			Clear     // Set all voxels to EMPTY
		}

		// ------------- Transform Operations -------------

		public enum ResizeType { Resize, Repeat, Rescale }
	}

	[Serializable]
	public abstract class VoxelMap<TVoxel> : VoxelMap
	{
		public abstract bool IsVoxelFilled(TVoxel v); 

		// ------------- GET Voxels -------------

		public abstract TVoxel GetVoxel(int x, int y, int z);
		public TVoxel GetVoxel(Vector3Int coordinate) => GetVoxel(coordinate.x, coordinate.y, coordinate.z);

		public bool TryGetVoxel(Vector3Int index, out TVoxel voxel)
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
			TryGetVoxel(coordinate, out TVoxel voxel) && IsVoxelFilled(voxel);

		// ------------- SET Voxels -------------
	}
}