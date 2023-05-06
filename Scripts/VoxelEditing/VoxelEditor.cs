using UnityEngine;
using MUtility;
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

	[RequireComponent(typeof(VoxelFilter), typeof(VoxelRenderer))]
	[ExecuteAlways]
	class VoxelEditor : MonoBehaviour, IVoxelEditor
	{
		[SerializeField, HideInInspector] VoxelFilter voxelFilter;
		[SerializeField, HideInInspector] VoxelRenderer voxelRenderer;

		[SerializeField, HideInInspector] internal TransformLock transformLock = new() { position = true, rotation = true, scale = true };
		[SerializeField, HideInInspector] internal VoxelAction selectedAction = VoxelAction.Attach;
		[SerializeField, HideInInspector] internal VoxelTool selectedTool = VoxelTool.None;
		[SerializeField, HideInInspector] internal int selectedPaletteIndex = 0;
		[SerializeField, HideInInspector] internal BoundsInt selection = new(Vector3Int.zero, Vector3Int.zero);

		public TransformLock TransformLock { get => transformLock; set => transformLock = value; }

		public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();
		bool HasConnectedMap() => voxelFilter != null && voxelFilter.HasConnectedMap();

		public BoundsInt Selection { get => selection; set => selection = value; }

		public Object MapContainer => voxelFilter == null ? null :
			voxelFilter.HasConnectedMap() ? voxelFilter.ConnectedVoxelMap : voxelFilter;

		public Object EditorObject => this;

		public bool EnableEdit => enabled;

		private void OnValidate()
		{
			voxelFilter = GetComponent<VoxelFilter>();
			voxelRenderer = GetComponent<VoxelRenderer>();
			VoxelPalette palette = VoxelPalette;
			if (palette == null)
				selectedPaletteIndex = 0;
			else
				selectedPaletteIndex = Mathf.Max(Mathf.Min(selectedPaletteIndex, palette.Length - 1), 0);
		}

		private static void DrawVoxel(Mesh mesh, Material mat, VoxelHit hit, Matrix4x4 transformMatrix)
		{
			var voxelMatrix = Matrix4x4.Translate(hit.voxelIndex + Vector3.one * 0.5f);
			Graphics.DrawMesh(mesh, transformMatrix * voxelMatrix, mat, 0);
		}

		private static void DrawCursor(Mesh mesh, Material mat, float scale, VoxelHit hit, Matrix4x4 transformMatrix)
		{
			var cursorRotation = Quaternion.LookRotation(hit.side.ToVector());
			var cursorMatrix = Matrix4x4.TRS(hit.hitWorldPosition, cursorRotation, Vector3.one * scale);
			Graphics.DrawMesh(mesh, transformMatrix * cursorMatrix, mat, 0);
		}

		public string MapName => HasConnectedMap() ? voxelFilter.ConnectedVoxelMap.name : voxelFilter.name;
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

		void OnDrawGizmos()
		{
			if (Map == null)
				return;

			Gizmos.matrix = transform.localToWorldMatrix;
			 
			Vector3Int mapSize = Map.FullSize;
			Color c = Color.white;

			if (this.HasSelection()) // Draw Selection
			{
				Gizmos.color = c;
				Vector3Int selectionSize = selection.size;
				Gizmos.DrawWireCube((Vector3)selection.min + (Vector3)selectionSize / 2f, selectionSize);

				c.a /= 4f;
			}
			Gizmos.color = c;
			Gizmos.DrawWireCube((Vector3)mapSize / 2f, mapSize);

			Gizmos.matrix = Matrix4x4.identity;
		}
	}
}
