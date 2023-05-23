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

		[SerializeField, HideInInspector] internal VoxelFilter voxelFilter;
		[SerializeField, HideInInspector] internal VoxelMeshGenerator meshGenerator;


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
			voxelFilter = GetComponent<VoxelFilter>();
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
			set => toolState = value;
		}

		// --- Palette ---
		public int SelectedPaletteIndex
		{
			get => selectedPaletteIndex;
			set => selectedPaletteIndex = value;
		}
		// public IVoxelPalette<IVoxelPaletteItem> VoxelPalette => voxelGenerator == null ? null : voxelGenerator.VoxelPalette;

		// --------------------------------------
		void Update()
		{
			DoLockTransform();
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

			if (this.HasSelection()) // Draw Selection
			{
				Gizmos.color = Color.yellow;
				const float offset = 0.05f;
				Vector3 selectionSize = selection.size + (2 * offset * Vector3.one);
				Gizmos.DrawWireCube((Vector3)selection.min - offset * Vector3.one + selectionSize / 2f, selectionSize);
			}

			Gizmos.matrix = Matrix4x4.identity;
		}

	}
}
