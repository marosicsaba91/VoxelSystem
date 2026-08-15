using MUtility;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace VoxelSystem
{
	[NoAutoStaticsCleanup]
	[CreateAssetMenu(fileName = "VoxelShapePalette", menuName = EditorConstants.categoryPath + "Voxel Shape Palette", order = EditorConstants.soOrder_Palette)]
	public class VoxelShapePalette : ScriptableObject
	{
		[FormerlySerializedAs("_items"),SerializeField] 
		List<VoxelShapeBuilder> items;

		// public IReadOnlyList<VoxelShapeBuilder> Shapes => _items;

		public int ItemCount => items.Count;

		public IEnumerable<int> GetVoxelIds()
		{
			foreach (VoxelShapeBuilder item in items)
				yield return item.VoxelId;
		}

		public IEnumerable<string> GetNames()
		{
			foreach (VoxelShapeBuilder item in items)
				yield return item.NiceName;
		}

		public int GetID(int index)
		{
			VoxelShapeBuilder builder = items.IndexClamped(index);
			if (builder != null)
				return builder.VoxelId;
			return 1;
		}


		public VoxelShapeBuilder GetBuilder(int id)
		{
			foreach (VoxelShapeBuilder item in items)
			{
				if (item.VoxelId == id)
					return item;
			}
			return GetDummyBuilder();
		}

		public bool TryGetBuilder(int id, out VoxelShapeBuilder builder)
		{
			foreach (VoxelShapeBuilder item in items)
			{
				if (item.VoxelId == id)
				{
					builder = item;
					return true;
				}
			}
			builder = GetDummyBuilder();
			return false;
		}

		public int GetIndexOf(int id)
		{
			for (int i = 0; i < items.Count; i++)
			{
				VoxelShapeBuilder item = items[i];
				if (item.VoxelId == id)
					return i;
			}
			return 1;
		}


		public VoxelShapeBuilder GetBuilderByIndex(int index) => items[index];


		static VoxelShapeBuilder _dummyBuilder;

		static VoxelShapeBuilder GetDummyBuilder()
		{
			if (_dummyBuilder == null)
			{
				_dummyBuilder = CreateInstance<VoxelShape_Cube>(); 
				_dummyBuilder.InitializeMeshCacheAndSave();
				_dummyBuilder.NiceName = "Dummy";
			}
			return _dummyBuilder;
		}

		internal bool ContainsID(int value) 
		{ 
			foreach (VoxelShapeBuilder item in items)
			{
				if (item.VoxelId == value)
					return true;
			}
			return false;
		}
	}
}