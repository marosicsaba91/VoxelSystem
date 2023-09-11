using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public abstract partial class VoxelNavAgentSetting : ScriptableObject
	{
		public abstract void GetPossiblePositions(VoxelMap voxelMap, List<Vector3Int> resultPositions);

		public abstract void SetupConnections(VoxelMap voxelMap, Dictionary<Vector3Int, NavVoxelData> navMap);
	}
}