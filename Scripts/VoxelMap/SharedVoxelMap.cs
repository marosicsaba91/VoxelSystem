using EasyEditor;
using UnityEngine;

namespace VoxelSystem
{
	public abstract class SharedVoxelMap : ScriptableObject
	{
		internal abstract VoxelMap Map { get; }

		public EasyProperty size = new (nameof(Size));
		protected Vector3Int Size => Map.FullSize;
	}

	public abstract class SharedVoxelMap<TVoxelMap> : SharedVoxelMap where TVoxelMap : VoxelMap, new()
	{
		[SerializeField, HideInInspector] internal TVoxelMap map;
		internal sealed override VoxelMap Map => map;
	}
}