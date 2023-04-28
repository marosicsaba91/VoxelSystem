using MUtility;
using System.IO; 
using UnityEngine;

namespace VoxelSystem
{
	[ExecuteAlways]
	class VoxelFilter : MonoBehaviour
	{
		[SerializeField] internal ArrayVoxelMap innerMap = null;
		[SerializeField, HideInInspector] VoxelRenderer voxelRenderer;
		[SerializeField, HideInInspector] internal SharedVoxelMap connectedMapHolder = null;

		[SerializeField] DisplayMember sharedMap = new(nameof(ConnectedVoxelMap));
		[SerializeField, HideInInspector] SharedVoxelMap _lastFrameConnectedMap = null;
		[SerializeField, HideIf(nameof(HaveConnectedMap))] DisplayMember exportVoxelMap = new(nameof(ExportVoxelMap));

		void OnValidate() 
		{
			voxelRenderer = GetComponent<VoxelRenderer>();
		}

		bool HaveConnectedMap => connectedMapHolder != null;

		public bool HasConnectedMap() => connectedMapHolder != null;

		public SharedVoxelMap ConnectedVoxelMap
		{
			get => connectedMapHolder;
			set
			{
				if (connectedMapHolder == value)
					return;
				if (value == null && connectedMapHolder != null)
				{ 
					innerMap = new();
					innerMap.SetupFrom(connectedMapHolder.Map);
					connectedMapHolder = null; 
				}
				else
				{
					connectedMapHolder = value;
					SetMeshDirty();
				} 
			}
		}

		public VoxelMap GetVoxelMap()
		{
			if (connectedMapHolder == null)
				return innerMap;
			else
				return connectedMapHolder.Map;
		}

		void Update() // ExecuteAlways
		{
			SubscribeToChange();
		}

		void OnEnable()
		{ 
			if (innerMap == null)
			{
				innerMap = new();
				innerMap.Setup();
				SetMeshDirty();
			}
			VoxelMap map = GetVoxelMap();
			if (map != null)
				map.MapChangedEvent += SetMeshDirty;
		}

		void OnDisable()
		{
			VoxelMap map = GetVoxelMap();
			if (map != null)
				map.MapChangedEvent -= SetMeshDirty;
		}

		void SubscribeToChange()
		{
			if (_lastFrameConnectedMap == connectedMapHolder)
				return;

			if (_lastFrameConnectedMap != null)
				_lastFrameConnectedMap.Map.MapChangedEvent -= SetMeshDirty;
			else if (innerMap != null)
				innerMap.MapChangedEvent -= SetMeshDirty;

			if (connectedMapHolder != null)
				connectedMapHolder.Map.MapChangedEvent += SetMeshDirty;
			else if (innerMap != null)
				innerMap.MapChangedEvent += SetMeshDirty;
			_lastFrameConnectedMap = connectedMapHolder;
		}

		void SetMeshDirty() 
		{
			if(voxelRenderer != null)
				voxelRenderer.RegenerateMesh();
		}


		void ExportVoxelMap()
		{
#if UNITY_EDITOR

			string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Voxel Map", "VoxelMap", "asset", "Save Voxel Map");
			if (path.Length != 0)
			{
				SharedArrayVoxel newMap = ScriptableObject.CreateInstance<SharedArrayVoxel>();
				newMap.map = innerMap.GetCopy();
				newMap.name = Path.GetFileName(path);
				UnityEditor.AssetDatabase.CreateAsset(newMap, path);
				UnityEditor.AssetDatabase.Refresh();
			}
#endif
		}
	}
}
