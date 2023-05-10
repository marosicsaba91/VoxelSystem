using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "OctVoxelMap", menuName = "VoxelSystem/OctVoxelMap", order = 1)]
	public class SharedOctVoxelMap : SharedVoxelMap<OctVoxelMap> { }
}