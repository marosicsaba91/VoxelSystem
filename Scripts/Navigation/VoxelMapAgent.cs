using EasyInspector;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[ExecuteAlways]
	public class VoxelMapAgent : MonoBehaviour
	{
		[SerializeField] VoxelNavMap navMap;
		[SerializeField] float speed;
		[SerializeField] Color color = Color.cyan;
		[SerializeField] EasyMember recalculatePath = new(nameof(RecalculatePath));

		int currentPathIndex;
		readonly List<Vector3> path = new();

		void Start()
		{
			RecalculatePath();
		}

		void Update()
		{			
			if (Application.isPlaying && path.Count > 1)
				Move();
			else
				RecalculatePath();			
		}

		void Move()
		{
			if (path.Count <= currentPathIndex)
				return;

			Vector3 pos = transform.position;
			Vector3 next = path[currentPathIndex];

			transform.position = Vector3.MoveTowards(pos, next, speed * Time.deltaTime);

			if (pos == next)
				currentPathIndex++;
		}

		void RecalculatePath()
		{
			if (navMap != null)
			{
				navMap.TryGetPath(transform.position, path);
				currentPathIndex = 0;
			}
		}

		void OnDrawGizmos()
		{
			Gizmos.color = color;
			for (int i = 0; i < path.Count - 1; i++)
			{
				Gizmos.DrawLine(path[i], path[i + 1]);
			}
		}
	}
}