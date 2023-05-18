using UnityEngine;
using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace VoxelSystem
{
	public enum ToolState { None, Down, Drag, Up }

	[Serializable]
	public struct TransformLock
	{
		public bool position;
		public bool rotation;
		public bool scale;
	}

	[RequireComponent(typeof(VoxelFilter))]
	[ExecuteAlways]
	class VoxelEditor : MonoBehaviour, IVoxelEditor
	{
		static VoxelAction selectedAction = VoxelAction.Attach;
		static VoxelTool selectedTool = VoxelTool.None;
		static ToolState toolState = ToolState.None;
		static int selectedPaletteIndex = 0;

		[SerializeField] internal VoxelFilter voxelFilter;
		[SerializeField] internal VoxelMeshGenerator meshGenerator;
		[SerializeField] internal VoxelMeshGenerator quickMeshGenerator;


		[SerializeField, HideInInspector] internal TransformLock transformLock = new();
		[SerializeField, HideInInspector] internal BoundsInt selection = new(Vector3Int.zero, Vector3Int.one * -1);

		public TransformLock TransformLock { get => transformLock; set => transformLock = value; }

		public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

		public BoundsInt Selection { get => selection; set => selection = value; }

		public Object MapContainer => voxelFilter == null ? null :
			voxelFilter.HasSharedMap ? voxelFilter.SharedVoxelMap : voxelFilter;

		public Object EditorObject => this;

		public bool EnableEdit => enabled;

		private void OnValidate()
		{
			if (voxelFilter == null)
				voxelFilter = GetComponent<VoxelFilter>();
			if (meshGenerator == null)
				meshGenerator = GetComponent<VoxelMeshGenerator>();
		}

		public int PaletteLength => meshGenerator == null ? 1 : meshGenerator.PaletteLength;
		public IEnumerable<IVoxelPaletteItem> PaletteItems
		{
			get
			{
				if (meshGenerator == null)
					yield break;
				foreach (IVoxelPaletteItem item in meshGenerator.PaletteItems)
					yield return (IVoxelPaletteItem)item;
			}
		}

		public string MapName => voxelFilter == null ? "-" : voxelFilter.MapName;
		public VoxelTool SelectedTool { get => selectedTool; set => selectedTool = value; }
		public VoxelAction SelectedAction { get => selectedAction; set => selectedAction = value; }
		public ToolState ToolState
		{
			get => toolState;
			set
			{
				if (toolState == value) return;
				toolState = value;
				if (value == ToolState.None && meshGenerator != null && meshGenerator != quickMeshGenerator)
					meshGenerator.RegenerateMeshes();
			}
		}

		// --- Palette ---
		public int SelectedPaletteIndex
		{
			get => selectedPaletteIndex;
			set => selectedPaletteIndex = value;
		}
		// public IVoxelPalette<IVoxelPaletteItem> VoxelPalette => voxelGenerator == null ? null : voxelGenerator.VoxelPalette;

		// --------------------------------------

		VoxelFilter _lastFilter;
		void Update()
		{
			DoLockTransform();

			if (voxelFilter != null)
			{
				voxelFilter.MapChanged -= OnMapChanged;
				voxelFilter.MapChanged += OnMapChanged;
				_lastFilter = voxelFilter;
			}
			else if (_lastFilter != null) 
			{
				_lastFilter.MapChanged -= OnMapChanged;
			}
		}

		void OnMapChanged() 
		{
			if(toolState is ToolState.Down or ToolState.Drag && quickMeshGenerator != null)
				quickMeshGenerator.RegenerateMeshes();
			else if(meshGenerator != null)
				meshGenerator.RegenerateMeshes();
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

		void OnDrawGizmosSelected()
		{
			if (voxelFilter == null)
				voxelFilter = GetComponent<VoxelFilter>();

			if (Map == null)
				return;

			Gizmos.matrix = transform.localToWorldMatrix;

			Vector3Int mapSize = Map.FullSize;

			Gizmos.color = this.HasSelection() ? new Color(1f, 1f, 1f, 0.25f) : Color.white;
			Gizmos.DrawWireCube((Vector3)mapSize / 2f, mapSize);


			if (this.HasSelection()) // Draw Selection
			{
				Gizmos.color = Color.yellow;
				const float offset = 0.05f;
				Vector3 selectionSize = selection.size + (2 * offset * Vector3.one);
				Gizmos.DrawWireCube((Vector3)selection.min - offset * Vector3.one + selectionSize / 2f, selectionSize);

				// const float offset2 = -0.025f;
				// selectionSize = selection.size + (2 * offset2 * Vector3.one);
				// Gizmos.DrawWireCube((Vector3)selection.min - offset2 * Vector3.one + selectionSize / 2f, selectionSize); 
			}

			Gizmos.matrix = Matrix4x4.identity;
		}

	}
}
