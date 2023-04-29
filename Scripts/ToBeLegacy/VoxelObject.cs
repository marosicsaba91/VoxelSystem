using System;
using UnityEngine; 
using System.Collections.Generic;

namespace VoxelSystem
{
	[ExecuteAlways]
	public class VoxelObject : MonoBehaviour, IVoxelEditable
	{
		// Map, Palette, Builder
		[SerializeField, HideInInspector] internal VoxelMapScriptableObject connectedMap = null;
		[SerializeField, HideInInspector] internal VoxelBuilder connectedBuilder = null;
		[SerializeField, HideInInspector] internal ArrayVoxelMap innerMap = null;

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

		[SerializeField] VoxelAction selectedAction = VoxelAction.Attach;
		[SerializeField] VoxelTool selectedTool = VoxelTool.None;
		[SerializeField] int selectedPaletteIndex = 0;


		int _meshDirtyCounter = 0;
		VoxelMapScriptableObject _lastFrameConnectedMap;

		void OnValidate() => MaintainReferences();

		internal bool HasConnectedMap() => connectedMap != null;
		public VoxelTool SelectedTool { get => selectedTool; set => selectedTool = value; }
		public VoxelAction SelectedAction { get => selectedAction; set => selectedAction = value; }
		public int SelectedPaletteIndex { get => selectedPaletteIndex; set => selectedPaletteIndex = value; }

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

		public VoxelBuilder ConnectedBuilder
		{
			get => connectedBuilder;

			set
			{
				if (connectedBuilder == value)
					return;
				connectedBuilder = value;
				SetMeshDirty();
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

		public UnityEngine.Object RecordableUnityObject => HasConnectedMap() ? ConnectedMap : this;

		public bool EnableEdit => enabled;

		public int PaletteLength => ConnectedBuilder == null ? 0 : ConnectedBuilder.PaletteLength;

		public string MapName => HasConnectedMap() ? ConnectedMap.name : name;

		public IEnumerable<PaletteItem> GetPaletteItems()
		{
			if(ConnectedBuilder == null)
				yield break;
			foreach (PaletteItem item in ConnectedBuilder.GetPaletteItems())
				yield return item;
		}

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
			ArrayVoxelMap map = ArrayMap;
			VoxelBuilder builder = connectedBuilder;

			if (map == null || builder == null)
			{ return; }
			Mesh mesh = builder.VoxelMapToMesh(map);

			if (mesh == null)
			{ return; }
			MaintainReferences();

			references.meshFilter.mesh = mesh;
			if (references.meshCollider != null)
			{ references.meshCollider.sharedMesh = mesh; }
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
			if (connectedBuilder == null)
			{ return; }
			if (ArrayMap == null)
			{ return; }

			Matrix4x4 oldMatrix = Gizmos.matrix;
			Gizmos.matrix = transform.localToWorldMatrix;
			connectedBuilder.DrawGizmos(ArrayMap);

			Gizmos.matrix = oldMatrix;
		}

	}
}