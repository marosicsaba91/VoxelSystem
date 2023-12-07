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

		public IEnumerable<uint> GetVoxelIds()
		{
			foreach (VoxelShapeBuilder item in _items)
				yield return item.VoxelId;
		}

		public IEnumerable<string> GetNames()
		{
			foreach (VoxelShapeBuilder item in _items)
				yield return item.NiceName;
		}

		public uint GetID(int index)
		{
			VoxelShapeBuilder builder = _items.IndexClamped(index);
			if (builder != null)
				return builder.VoxelId;
			return 1;
		}


		public VoxelShapeBuilder GetBuilder(uint id)
		{
			foreach (VoxelShapeBuilder item in _items)
			{
				if (item.VoxelId == id)
					return item;
			}
			return GetDummyBuilder();
		}

		public int GetIndexOf(uint id)
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
				dummyBuilder.InitializeAndSetupPreview();
				dummyBuilder.NiceName = "Dummy";
			}
			return dummyBuilder;
		} 
	}
}