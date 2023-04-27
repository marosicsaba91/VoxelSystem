using MUtility;
using System;
using UnityEngine;

namespace VoxelSystem
{
	[ExecuteAlways]
	class VoxelFilter : MonoBehaviour
	{
		 
		[SerializeField, HideInInspector] internal OctVoxelMap innerOctMap = null;
		[SerializeField, HideInInspector] VoxelRenderer voxelRenderer;
		[SerializeField, HideInInspector] internal VoxelMapScriptableObject connectedSOMap = null;

		[SerializeField] DisplayMember connectedMap = new(nameof(ConnectedVoxelMap));
		[SerializeField, HideInInspector] VoxelMapScriptableObject _lastFrameConnectedMap = null;

		void OnValidate() 
		{
			voxelRenderer = GetComponent<VoxelRenderer>();
		}

		public bool HasConnectedMap() => connectedSOMap != null;

		public VoxelMapScriptableObject ConnectedVoxelMap
		{
			get => connectedSOMap;
			set
			{
				if (connectedSOMap == value)
					return;
				if (value == null)
				{
					innerOctMap = (OctVoxelMap)connectedSOMap.octMap.GetCopy();
					innerOctMap.DeserializeFromByteArray();
					connectedSOMap = null;
				}
				else
				{
					connectedSOMap = value;
					SetMeshDirty();
				} 
			}
		}

		public OctVoxelMap GetOctMap()
		{
			if (connectedSOMap == null)
				return innerOctMap;
			else
				return connectedSOMap.octMap;
		}

		void Update() // ExecuteAlways
		{
			SubscribeToChange();
		}

		void OnEnable()
		{ 
			if (innerOctMap == null)
			{
				innerOctMap = new();
				SetMeshDirty();
			}
			OctVoxelMap map = GetOctMap();
			if (map != null)
				map.MapChangedEvent += SetMeshDirty;
		}

		void OnDisable()
		{
			OctVoxelMap map = GetOctMap();
			if (map != null)
				map.MapChangedEvent -= SetMeshDirty;
		}

		void SubscribeToChange()
		{
			if (_lastFrameConnectedMap == connectedSOMap)
				return;

			if (_lastFrameConnectedMap != null)
				_lastFrameConnectedMap.map.MapChangedEvent -= SetMeshDirty;
			else if (innerOctMap != null)
				innerOctMap.MapChangedEvent -= SetMeshDirty;

			if (connectedSOMap != null)
				connectedSOMap.map.MapChangedEvent += SetMeshDirty;
			else if (innerOctMap != null)
				innerOctMap.MapChangedEvent += SetMeshDirty;
			_lastFrameConnectedMap = connectedSOMap;
		}

		void SetMeshDirty() 
		{
			voxelRenderer.RegenerateMesh();
		}

	}
}
