using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[CreateAssetMenu(fileName = "WalkingAgentSetting", menuName = "Voxel System/Walking Agent Setting")]
public class WalkingAgentSetting : VoxelNavAgentSetting
{
	[SerializeField] DirectionType allowedDirections = DirectionType.General;

	public sealed override void GetPossiblePositions(VoxelMap voxelMap, List<Vector3Int> resultPositions)
	{
		resultPositions.Clear();
		Vector3Int size = voxelMap.FullSize;
		for (int x = 0; x < size.x; x++)
			for (int y = 0; y < size.y; y++)
				for (int z = 0; z < size.z; z++)
				{
					if (voxelMap.GetVoxel(x, y, z).IsFilled()) continue;
					if (!voxelMap.TryGetVoxel(x, y - 1, z, out int under)) continue;
					if (under.IsEmpty()) continue;
					resultPositions.Add(new Vector3Int(x, y, z));
				}
	}

	public sealed override void SetupConnections(VoxelMap voxelMap, Dictionary<Vector3Int, NavVoxelData> navMap)
	{
		Vector3Int[] allowedDirectionVectors = allowedDirections.GetDirectionVectors();
		foreach ((Vector3Int indexPos, NavVoxelData data) in navMap)
		{
			foreach (Vector3Int directionVector in allowedDirectionVectors)
			{
				Vector3Int neighbourIndex = indexPos + directionVector;
				if (navMap.TryGetValue(neighbourIndex, out NavVoxelData neighbourVoxel))
					data.AddConnection(neighbourVoxel);
			}
		}
	}
}
