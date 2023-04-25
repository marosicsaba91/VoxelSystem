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
			voxelRenderer = GetComponent<VoxelRenderer>();
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
	}
}
