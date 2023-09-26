using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "VoxelMap", menuName = EditorConstants.categoryPath + "Voxel Map", order = EditorConstants.soOrder_VoxelMap)]
	public class SharedArrayVoxelMap : SharedVoxelMap<ArrayVoxelMap> { }
}