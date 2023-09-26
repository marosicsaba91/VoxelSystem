using UnityEngine;
using System.Collections.Generic;

namespace VoxelSystem
{
	[ExecuteAlways]
	public class VoxelNavTarget : MonoBehaviour
	{
		[SerializeField] VoxelObject filter;
		[SerializeField] Vector3Int targetPoint;
		[SerializeField] Color color = Color.cyan;

		static readonly Dictionary<VoxelObject, VoxelNavTarget> allTargets = new();

		public Vector3Int TargetPoint => targetPoint;

		Vector3 GlobalPosition =>
			filter.transform.TransformPoint(targetPoint) + Vector3.one * 0.5f;

		void Update()
		{
			if (Application.isPlaying) return;
			if (filter == null) return;

			Vector3 indexF = filter.transform.InverseTransformPoint(transform.position - (Vector3.one * 0.5f));
			targetPoint = Vector3Int.RoundToInt(indexF);
		}

		void OnEnable()
		{
			if (filter == null) return;

			if (!allTargets.ContainsKey(filter))
				allTargets.Add(filter, this);
			if (allTargets[filter] != this)
				allTargets[filter] = this;
		}

		void OnDrawGizmos()
		{
			if (filter == null) return;

			Gizmos.color = color;
			Gizmos.DrawSphere(GlobalPosition, 0.5f);
		}



	}
}
