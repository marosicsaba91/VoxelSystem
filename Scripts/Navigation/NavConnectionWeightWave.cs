using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public class NavConnectionWeightWave
	{
		public HashSet<NavVoxelData> CurrentLevel { get; } = new();
		readonly HashSet<NavVoxelData> nextLevel = new();
		readonly HashSet<NavVoxelData> alreadyDone = new();

		public void Clear()
		{
			CurrentLevel.Clear();
			nextLevel.Clear();
			alreadyDone.Clear();
		}

		public void SetupTarget(NavVoxelData startPoint)
		{
			CurrentLevel.Add(startPoint);
			startPoint.cost = 0;
		}

		public bool NextLevel()
		{
			nextLevel.Clear();
			foreach (NavVoxelData voxel in CurrentLevel)
			{
				alreadyDone.Add(voxel);
				float cost = voxel.cost;
				foreach (NavVoxelData neighbour in voxel.neighbours)
				{
					if (alreadyDone.Contains(neighbour)) continue;
					float distance = GetDistance(voxel.indexPoint, neighbour.indexPoint);
					float newCost = cost + distance;

					if (neighbour.cost < 0 || newCost < neighbour.cost)
					{
						neighbour.cost = newCost;
						nextLevel.Add(neighbour);
					}
				}
			}
			CurrentLevel.Clear();

			foreach (NavVoxelData voxel in nextLevel)
				CurrentLevel.Add(voxel);

			return CurrentLevel.Count > 0;
		}

		public void CalculateValuesQuick()
		{
			int maxIterations = 1000;
			int iterations = 0;
			while (CurrentLevel.Count > 0 && iterations < maxIterations)
			{
				NextLevel();
				iterations++;
			}

			if (iterations >= maxIterations)
				Debug.LogError("NavConnectionWeightWave.FastWave() reached max iterations");
		}

		static readonly Dictionary<Vector3Int, float> distanceCosts = new();

		static float GetDistance(Vector3Int p1, Vector3Int p2)
		{
			Vector3Int distanceVec = p1 - p2;
			distanceVec = distanceVec.Abs();
			if (distanceCosts.TryGetValue(distanceVec, out float distance))
				return distance;
			distance = Vector3.Distance(p1, p2);
			distanceCosts.Add(distanceVec, distance);
			return distance;
		}
	}
}