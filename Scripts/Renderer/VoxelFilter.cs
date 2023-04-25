using System;
using UnityEngine;

namespace VoxelSystem
{
	class VoxelFilter : MonoBehaviour
	{

		[SerializeField, HideInInspector] internal VoxelMap innerMap = null;
		[SerializeField, HideInInspector] internal OctVoxelMap innerOctMap = null;
		[SerializeField, HideInInspector] VoxelRenderer voxelRenderer;
		[SerializeField] internal VoxelMapScriptableObject connectedMap = null;

		void OnValidate() 
		{			
			if (TryGetComponent(out voxelRenderer))
				voxelRenderer.RebuildMesh(); 
		}

		public bool HasConnectedMap() => connectedMap != null;
		public VoxelMap GetMap()
		{
			if (connectedMap == null)
				return innerMap;
			else
				return connectedMap.map;
		}

		public OctVoxelMap GetOctMap()
		{
			if (connectedMap == null)
				return innerOctMap;
			else
				return connectedMap.octMap;
		}

		public VoxelMapScriptableObject ConnectedMap
		{
			get => connectedMap;
			set
			{
				if (connectedMap == value)
					return;
				if (value == null)
					innerMap = connectedMap.map.GetCopy();
				else if (voxelRenderer != null)
					voxelRenderer.RebuildMesh();
				connectedMap = value;
			}
		}

	}
}
