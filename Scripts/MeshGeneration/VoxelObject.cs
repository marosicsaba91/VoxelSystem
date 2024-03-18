using EasyEditor;
using System.IO; 
using UnityEngine;

namespace VoxelSystem
{
	[ExecuteAlways]
	partial class VoxelObject : MonoBehaviour
	{
		[SerializeField, HideInInspector] ArrayVoxelMap innerMap = null;
		[SerializeField, HideInInspector] SharedVoxelMap sharedVoxelMap = null;
		[Header("Voxel Map")]
		[SerializeField] EasyProperty sharedMap = new(nameof(SharedVoxelMap));
		[SerializeField, DisableIf(nameof(HasSharedMap))] EasyProperty exportVoxelMapAsAsset =
			new(nameof(ExportVoxelMap));
		[SerializeField, HideInInspector] SharedVoxelMap _lastFrameSharedMap = null;

		public event MapChangedDelegate MapChanged;

		void OnMapChanged(bool isFinal)
		{
			MapChanged?.Invoke(isFinal);
			RegenerateMeshesAndUpdateMeshComponents(isFinal);
		}

		public string MapName => HasSharedMap ? SharedVoxelMap.name : name;

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
					OnMapChanged(isFinal: true);
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
			innerMap = map;
			sharedVoxelMap = null;
			innerMap.MapChangedEvent += OnMapChanged;
		}

		void Update() // ExecuteAlways
		{
			if (Application.isPlaying) return;

			EditorUpdate_VoxelMap();
			EditorUpdate_MeshGeneration();
		}

		void OnEnable()
		{
			if (innerMap == null)
			{
				innerMap = new();
				innerMap.Setup();
				OnMapChanged(isFinal: true);
			}
			VoxelMap map = GetVoxelMap();
			if (map != null)
				map.MapChangedEvent += OnMapChanged;
		}

		void OnDisable()
		{
			VoxelMap map = GetVoxelMap();
			if (map != null)
				map.MapChangedEvent -= OnMapChanged;
		}

		void EditorUpdate_VoxelMap()
		{
			if (_lastFrameSharedMap == null && sharedVoxelMap == null)
			{
				innerMap.MapChangedEvent -= OnMapChanged;
				innerMap.MapChangedEvent += OnMapChanged;
			}

			if (_lastFrameSharedMap == sharedVoxelMap)
				return;

			if (_lastFrameSharedMap != null)
				_lastFrameSharedMap.Map.MapChangedEvent -= OnMapChanged;
			else if (innerMap != null)
				innerMap.MapChangedEvent -= OnMapChanged;

			if (sharedVoxelMap != null)
				sharedVoxelMap.Map.MapChangedEvent += OnMapChanged;
			else if (innerMap != null)
				innerMap.MapChangedEvent += OnMapChanged;

			_lastFrameSharedMap = sharedVoxelMap;
		}

		void ExportVoxelMap()
		{
#if UNITY_EDITOR
			string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Voxel Map", "VoxelMap", "asset", "Save Voxel Map");
			if (path.Length != 0)
			{
				SharedArrayVoxelMap newMap = ScriptableObject.CreateInstance<SharedArrayVoxelMap>();

				newMap.map ??= new();
				newMap.map.SetupFrom(innerMap);

				newMap.name = Path.GetFileName(path);
				UnityEditor.AssetDatabase.CreateAsset(newMap, path);
				UnityEditor.AssetDatabase.Refresh();
			}
#endif
		}

		void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			VoxelMap map = GetVoxelMap();

			if (map == null)
				return;

			Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
			Vector3 mapSize = map.FullSize;
			Gizmos.DrawWireCube(mapSize / 2f, mapSize);
			Gizmos.matrix = Matrix4x4.identity;
		}

		public bool Raycast(Ray globalRay, out VoxelHit hit, bool returnOutsideVoxel = false)
		{
			VoxelMap map = GetVoxelMap();
			if (map == null)
			{
				hit = default;
				return false;
			}

			return map.Raycast(globalRay, out hit, transform, returnOutsideVoxel);
		}
	}
}