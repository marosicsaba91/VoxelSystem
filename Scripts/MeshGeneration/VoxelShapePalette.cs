using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "VoxelShapePalette", menuName = EditorConstants.categoryPath + "Voxel Shape Palette", order = EditorConstants.soOrder_Palette)]
	public class VoxelShapePalette : ScriptableObject
	{
		[SerializeField] List<VoxelShapeBuilder> _items;

		// public IReadOnlyList<VoxelShapeBuilder> Shapes => _items;

		public int ItemCount => _items.Count;

		public IEnumerable<int> GetVoxelIds()
		{
			foreach (VoxelShapeBuilder item in _items)
				yield return item.VoxelId;
		}

		public IEnumerable<string> GetNames()
		{
			foreach (VoxelShapeBuilder item in _items)
				yield return item.NiceName;
		}

		public int GetID(int index)
		{
			VoxelShapeBuilder builder = _items.IndexClamped(index);
			if (builder != null)
				return builder.VoxelId;
			return 1;
		}


		public VoxelShapeBuilder GetBuilder(int id)
		{
			foreach (VoxelShapeBuilder item in _items)
			{
				if (item.VoxelId == id)
					return item;
			}
			return GetDummyBuilder();
		}

		public bool TryGetBuilder(int id, out VoxelShapeBuilder builder)
		{
			foreach (VoxelShapeBuilder item in _items)
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
			for (int i = 0; i < _items.Count; i++)
			{
				VoxelShapeBuilder item = _items[i];
				if (item.VoxelId == id)
					return i;
			}
			return 1;
		}


		public VoxelShapeBuilder GetBuilderByIndex(int index) => _items[index];


		static VoxelShapeBuilder dummyBuilder;

		static VoxelShapeBuilder GetDummyBuilder()
		{
			if (dummyBuilder == null)
			{
				dummyBuilder = CreateInstance<VoxelShape_Cube>(); 
				dummyBuilder.InitializeMeshCacheAndSave();
				dummyBuilder.NiceName = "Dummy";
			}
			return dummyBuilder;
		}

		internal bool ContainsID(int value) 
		{ 
			foreach (VoxelShapeBuilder item in _items)
			{
				if (item.VoxelId == value)
					return true;
			}
			return false;
		}
	}
}