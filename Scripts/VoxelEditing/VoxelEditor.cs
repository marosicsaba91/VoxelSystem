using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace VoxelSystem
{
	[Serializable]
	public struct TransformLock
	{
		public bool position;
		public bool rotation;
		public bool scale;
	}

	[RequireComponent(typeof(VoxelFilter), typeof(BlockMeshGenerator))]
	[ExecuteAlways]
	class VoxelEditor : MonoBehaviour, IVoxelEditor
	{
		static VoxelAction selectedAction = VoxelAction.Attach;
		static VoxelTool selectedTool = VoxelTool.None;

		[SerializeField, HideInInspector] VoxelFilter voxelFilter;
		[SerializeField, HideInInspector] BlockMeshGenerator voxelRenderer;

		[SerializeField, HideInInspector] internal TransformLock transformLock = new();
		[SerializeField, HideInInspector] internal int selectedPaletteIndex = 0;
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
			voxelRenderer = GetComponent<BlockMeshGenerator>();
			VoxelPalette palette = VoxelPalette;
			if (palette == null)
				selectedPaletteIndex = 0;
			else
				selectedPaletteIndex = Mathf.Max(Mathf.Min(selectedPaletteIndex, palette.Length - 1), 0);
		}

		public string MapName => voxelFilter == null ? "-" : voxelFilter.MapName;
		public VoxelTool SelectedTool { get => selectedTool; set => selectedTool = value; }
		public VoxelAction SelectedAction { get => selectedAction; set => selectedAction = value; }

		// --- Palette ---
		public int SelectedPaletteIndex
		{
			get => selectedPaletteIndex;
			set => selectedPaletteIndex = value;
		}
		public VoxelPalette VoxelPalette => voxelRenderer == null ? null : voxelRenderer.VoxelPalette;

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
			if(voxelFilter == null)
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
