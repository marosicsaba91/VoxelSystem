using MUtility;
using System.IO; 
using UnityEngine;
using UnityEngine.Serialization;

namespace VoxelSystem
{
	[ExecuteAlways]
	class VoxelFilter : MonoBehaviour
	{
		[SerializeField, HideInInspector] ArrayVoxelMap innerMap = null;
		[SerializeField, HideInInspector] VoxelRenderer voxelRenderer;
		[SerializeField, HideInInspector, FormerlySerializedAs("connectedMapHolder")] SharedVoxelMap sharedVoxelMap = null;

		[SerializeField] DisplayMember sharedMap = new(nameof(SharedVoxelMap));
		[SerializeField, DisableIf(nameof(HasSharedMap))] DisplayMember exportVoxelMap = new(nameof(ExportVoxelMap));

		[SerializeField, HideInInspector] SharedVoxelMap _lastFrameSharedMap = null;

		internal void OnValidate() 
		{
			voxelRenderer = GetComponent<VoxelRenderer>();
		}

		public bool HasSharedMap => sharedVoxelMap != null; 

		public SharedVoxelMap SharedVoxelMap
		{
			get => sharedVoxelMap;
			set
			{
				if (sharedVoxelMap == value)
					return;
				if (value == null && sharedVoxelMap != null)
				{
					innerMap ??= new();
					innerMap.SetupFrom(sharedVoxelMap.Map);
					sharedVoxelMap = null; 
				}
				else
				{
					sharedVoxelMap = value;
					MapChanged();
				} 
			}
		}

		public VoxelMap GetVoxelMap()
		{
			if (sharedVoxelMap == null)
				return innerMap;
			else
				return sharedVoxelMap.Map;
		}

		internal void SetVoxelMap(ArrayVoxelMap map)
		{
			voxelRenderer = GetComponent<VoxelRenderer>();
			innerMap = map;
			sharedVoxelMap = null;
			innerMap.MapChangedEvent += MapChanged;
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
			if (_lastFrameSharedMap == null && sharedVoxelMap == null)
			{
				innerMap.MapChangedEvent -= MapChanged;
				innerMap.MapChangedEvent += MapChanged;
			}

			if (_lastFrameSharedMap == sharedVoxelMap)
				return;

			if (_lastFrameSharedMap != null)
				_lastFrameSharedMap.Map.MapChangedEvent -= MapChanged;
			else if (innerMap != null)
				innerMap.MapChangedEvent -= MapChanged;

			if (sharedVoxelMap != null)
				sharedVoxelMap.Map.MapChangedEvent += MapChanged;
			else if (innerMap != null)
				innerMap.MapChangedEvent += MapChanged;

			_lastFrameSharedMap = sharedVoxelMap;
		}


		void MapChanged()
		{
			if (voxelRenderer != null || TryGetComponent(out voxelRenderer))
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
