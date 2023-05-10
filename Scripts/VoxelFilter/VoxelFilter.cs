using MUtility;
using System.IO; 
using UnityEngine;

namespace VoxelSystem
{
	[ExecuteAlways]
	class VoxelFilter : MonoBehaviour
	{
		[SerializeField, HideInInspector] ArrayVoxelMap innerMap = null;
		[SerializeField, HideInInspector] VoxelRenderer voxelRenderer;
		[SerializeField, HideInInspector] SharedVoxelMap connectedMapHolder = null;

		[SerializeField] DisplayMember sharedMap = new(nameof(ConnectedVoxelMap));
		[SerializeField, HideInInspector] SharedVoxelMap _lastFrameConnectedMap = null;
		[SerializeField, DisableIf(nameof(HaveConnectedMap))] DisplayMember exportVoxelMap = new(nameof(ExportVoxelMap));

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
					innerMap ??= new();
					innerMap.SetupFrom(connectedMapHolder.Map);
					connectedMapHolder = null; 
				}
				else
				{
					connectedMapHolder = value;
					MapChanged();
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
				MapChanged();
			}
			VoxelMap map = GetVoxelMap();
			if (map != null)
				map.MapChangedEvent += MapChanged;
		}

		void OnDisable()
		{
			VoxelMap map = GetVoxelMap();
			if (map != null)
				map.MapChangedEvent -= MapChanged;
		}

		void SubscribeToChange()
		{
			// Debug.Log("Subscribe");

			if (_lastFrameConnectedMap == connectedMapHolder)
				return;

			if (_lastFrameConnectedMap != null)
				_lastFrameConnectedMap.Map.MapChangedEvent -= MapChanged;
			else if (innerMap != null)
				innerMap.MapChangedEvent -= MapChanged;

			if (connectedMapHolder != null)
				connectedMapHolder.Map.MapChangedEvent += MapChanged;
			else if (innerMap != null)
				innerMap.MapChangedEvent += MapChanged;
			_lastFrameConnectedMap = connectedMapHolder;
		}


		void MapChanged()
		{
			if (voxelRenderer != null)
				voxelRenderer.RegenerateMesh();
		}

		void ExportVoxelMap()
		{
#if UNITY_EDITOR

			string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Voxel Map", "VoxelMap", "asset", "Save Voxel Map");
			if (path.Length != 0)
			{
				SharedArrayVoxel newMap = ScriptableObject.CreateInstance<SharedArrayVoxel>();

				newMap.map ??= new();
				newMap.map.SetupFrom(innerMap);

				newMap.name = Path.GetFileName(path);
				UnityEditor.AssetDatabase.CreateAsset(newMap, path);
				UnityEditor.AssetDatabase.Refresh();
			}
#endif
		}
	}
}
