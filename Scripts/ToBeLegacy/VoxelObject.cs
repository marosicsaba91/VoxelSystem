using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace VoxelSystem
{
	[ExecuteAlways]
	public class VoxelObject : MonoBehaviour, IVoxelEditor
	{
		[SerializeField, HideInInspector] internal VoxelMapScriptableObject connectedMap;
		[SerializeField, HideInInspector] internal ArrayVoxelMap innerMap;
		[SerializeField, HideInInspector] internal BoundsInt selection = new();
		[SerializeField] BlockLibrary blockLibrary;
		[SerializeField] VoxelPalette voxelPalette;

		[Serializable]
		public struct References
		{
			public MeshRenderer meshRenderer;
			public MeshFilter meshFilter;
			public MeshCollider meshCollider;
		}
		public References references = new();
		[SerializeField] TransformLock transformLock = new() { position = true, rotation = true, scale = true };

		public TransformLock TransformLock { get => transformLock; set => transformLock = value; }

		[SerializeField] bool mergeCloseEdgesOnTestMesh = true;

		[SerializeField] VoxelAction selectedAction = VoxelAction.Attach;
		[SerializeField] VoxelTool selectedTool = VoxelTool.None;
		[SerializeField] int selectedPaletteIndex = 0;

		public BoundsInt Selection { get => selection; set => selection = value; }


		int _meshDirtyCounter = 0;
		VoxelMapScriptableObject _lastFrameConnectedMap;

		void OnValidate() => MaintainReferences();

		internal bool HasConnectedMap() => connectedMap != null;
		public VoxelTool SelectedTool { get => selectedTool; set => selectedTool = value; }
		public VoxelAction SelectedAction { get => selectedAction; set => selectedAction = value; }
		public int SelectedPaletteIndex { get => selectedPaletteIndex; set => selectedPaletteIndex = value; }

		public VoxelPalette VoxelPalette { get => voxelPalette; }
		internal void SetPalette(VoxelPalette value) => voxelPalette = value;

		public VoxelMapScriptableObject ConnectedMap
		{
			get => connectedMap;
			set
			{
				if (connectedMap == value)
					return;
				if (value == null)
				{
					innerMap ??= new();
					innerMap.SetupFrom(connectedMap.map);
				}
				else
				{
					SetMeshDirty();
				}
				connectedMap = value;
			}
		}

		public ArrayVoxelMap ArrayMap
		{
			get => connectedMap != null ? connectedMap.map : innerMap;
			set
			{
				if (connectedMap != null)
				{ connectedMap.map = value; }
				else if (innerMap != null)
				{ innerMap = value; }
			}
		}

		public VoxelMap Map => ArrayMap;

		public Object MapContainer => HasConnectedMap() ? connectedMap : this;
		public Object EditorObject => HasConnectedMap() ? connectedMap : this;

		public bool EnableEdit => enabled;

		public string MapName => HasConnectedMap() ? ConnectedMap.name : name;

		public IEnumerable<PaletteItem> PaletteItems() => voxelPalette.Items;
		public int PaletteLength => voxelPalette == null ? 0 : voxelPalette.Length;


		void OnEnable()
		{
			MaintainReferences();
			if (innerMap == null)
			{
				innerMap = new();
				innerMap.Setup();
				SetMeshDirty();
			}
			ArrayVoxelMap map = ArrayMap;
			if (map != null)
				map.MapChangedEvent += SetMeshDirty;
		}

		void OnDisable()
		{
			ArrayVoxelMap map = ArrayMap;
			if (map != null)
				map.MapChangedEvent -= SetMeshDirty;
		}

		// Update is called once per frame
		void Update()
		{
			SubscribeToChange();
			DoLockTransform();
		}

		void SubscribeToChange()
		{
			if (_lastFrameConnectedMap == connectedMap)
				return;

			if (_lastFrameConnectedMap != null)
				_lastFrameConnectedMap.map.MapChangedEvent -= SetMeshDirty;
			else if (innerMap != null)
				innerMap.MapChangedEvent -= SetMeshDirty;

			if (connectedMap != null)
				connectedMap.map.MapChangedEvent += SetMeshDirty;
			else if (innerMap != null)
				innerMap.MapChangedEvent += SetMeshDirty;
			_lastFrameConnectedMap = connectedMap;
			SetMeshDirty();
		}

		void DoLockTransform()
		{
			if (TransformLock.position)
			{
				Vector3 lp = transform.localPosition;
				transform.localPosition = new Vector3(Mathf.RoundToInt(lp.x), Mathf.RoundToInt(lp.y), Mathf.RoundToInt(lp.z));
			}
			if (TransformLock.rotation)
			{
				Vector3 lr = transform.localRotation.eulerAngles;
				transform.localRotation = Quaternion.Euler(new Vector3(Mathf.RoundToInt(lr.x / 90f) * 90, Mathf.RoundToInt(lr.y / 90f) * 90, Mathf.RoundToInt(lr.z / 90f) * 90));
			}
			if (TransformLock.scale)
			{
				Vector3 ls = transform.localScale;
				transform.localScale = new Vector3(Mathf.RoundToInt(ls.x), Mathf.RoundToInt(ls.y), Mathf.RoundToInt(ls.z));
			}
		}

		void LateUpdate()
		{
			if (_meshDirtyCounter <= 0)
				return;

			RegenerateMesh();
			_meshDirtyCounter = 0;
		}

		public void RegenerateMesh()
		{
			VoxelMap map = ArrayMap;
			if (map == null) return;
			if (blockLibrary == null) return;

			MaintainReferences();

			Mesh mesh = references.meshFilter.sharedMesh;
			if (mesh == null) return;
			mesh = VoxelBuilder.VoxelMapToMesh(map, GenerateMesh);
			references.meshFilter.sharedMesh = mesh;

			if (references.meshCollider != null)
				references.meshCollider.sharedMesh = mesh;
		}


		static readonly List<Block> _blockCache = new();
		void GenerateMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
		{
			if (voxelMap == null) return;
			BlockVoxelBuilder.CalculateBlocks(voxelMap, _blockCache, mergeCloseEdgesOnTestMesh);

			BlockVoxelBuilder.BuildMeshFromBlocks(blockLibrary, _blockCache, vertices, normals, uv, triangles);
			Debug.Log("Mesh Regenerated");
		}

		void MaintainReferences()
		{
			if (references.meshFilter.Equals(null))
				references.meshFilter = GetComponent<MeshFilter>();
			if (references.meshRenderer.Equals(null))
				references.meshRenderer = GetComponent<MeshRenderer>();
			if (references.meshCollider.Equals(null))
				references.meshCollider = GetComponent<MeshCollider>();

		}

		void SetMeshDirty()
		{
			if (Application.isPlaying)
				_meshDirtyCounter++;
			else
				RegenerateMesh();
		}

		void OnDrawGizmosSelected()
		{
			if (ArrayMap == null)
			{ return; }

			Matrix4x4 oldMatrix = Gizmos.matrix;
			Gizmos.matrix = transform.localToWorldMatrix;
			// connectedBuilder.DrawGizmos(ArrayMap);

			Gizmos.matrix = oldMatrix;
		}

	}
}