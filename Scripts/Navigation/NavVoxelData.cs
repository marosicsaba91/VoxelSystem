using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public class NavVoxelData
	{
		const float unreachableCost = -1;
		Vector3 half = Vector3.one / 2f;

		public Vector3Int indexPoint;
		public Vector3 LocalPoint => indexPoint + half;
		public Vector3 GlobalPoint(Transform t) => t.TransformPoint(LocalPoint);

		public List<NavVoxelData> neighbours;
		public float cost = unreachableCost;

		public NavVoxelData(Vector3Int index)
		{
			indexPoint = index;
			neighbours = new List<NavVoxelData>();
		}

		public void SetAsTarget()
		{
			cost = 0;
		}

		public void AddConnection(NavVoxelData neighbour)
		{
			if (!neighbours.Contains(this))
				neighbours.Add(neighbour);
		}

		public NavVoxelData GetMinimalCostNeighbour()
		{
			float minCost = float.MaxValue;
			NavVoxelData minCostGate = null;
			for (int i = 0; i < neighbours.Count; i++)
			{
				NavVoxelData neighbour = neighbours[i];
				if (neighbour == null) continue;
				float neighbourCost = neighbour.cost;
				if (neighbourCost < 0) continue;
				if (neighbourCost > cost) continue;
				if (neighbourCost >= minCost) continue;

				minCost = neighbour.cost;
				minCostGate = neighbour;
			}
			return minCostGate;
		}

	}
}