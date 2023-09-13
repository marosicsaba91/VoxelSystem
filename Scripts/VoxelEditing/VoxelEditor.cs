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

	[RequireComponent(typeof(VoxelObject))]
	[ExecuteAlways]
	class VoxelEditor : MonoBehaviour, IVoxelEditor
	{
		static VoxelAction selectedAction = VoxelAction.Attach;
		static VoxelTool selectedTool = VoxelTool.None;
		static ToolState toolState = ToolState.None;
		// static int selectedMaterialIndex = 0;
		// static int selectedVoxelTypeIndex = 0;
		static int selectedVoxelValue = 0;

		[SerializeField] MaterialPalette materialPalette;

		[SerializeField, HideInInspector] internal VoxelObject voxelFilter;
		[SerializeField, HideInInspector] internal VoxelMeshGenerator meshGenerator;
		[SerializeField, HideInInspector] internal UniversalMeshGenerator universalMeshGenerator;
		[SerializeField, HideInInspector] internal MeshRenderer meshRenderer;
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
				voxelFilter = GetComponent<VoxelObject>();

			if (meshGenerator == null)
				meshGenerator = GetComponent<VoxelMeshGenerator>();

			if (universalMeshGenerator == null)
				universalMeshGenerator = GetComponent<UniversalMeshGenerator>();

			if (meshRenderer == null)
				meshRenderer = GetComponent<MeshRenderer>();

			FreshRendererMaterialPalette();
		}

		void FreshRendererMaterialPalette()
		{
			if (meshRenderer == null || materialPalette == null) return;

			List<Material> materials = new();

			for (int i = 0; i < materialPalette.Count; i++)
				materials.Add(materialPalette[i].Material);

			meshRenderer.SetMaterials(materials);
		}

		public IReadOnlyList<MaterialSetup> MaterialPaletteItems => materialPalette != null ? materialPalette.Materials : null;
		public IPalette MaterialPalette => universalMeshGenerator.MaterialPalette;

		public IPalette VoxelTypePalette => universalMeshGenerator.VoxelTypePalette;

		public string MapName => voxelFilter == null ? "-" : voxelFilter.MapName;
		public VoxelTool SelectedTool { get => selectedTool; set => selectedTool = value; }
		public VoxelAction SelectedAction { get => selectedAction; set => selectedAction = value; }
		public ToolState ToolState
		{
			get => toolState;
			set => toolState = value;
		}

		public int SelectedVoxelValue => selectedVoxelValue;

		// --- Material Palette ---
		public int SelectedMaterialIndex
		{
			get => selectedVoxelValue.GetMaterialIndex();
			set => selectedVoxelValue.SetMaterialIndex((byte)value);
		}

		// --- VoxelType Palette ---

		public int SelectedVoxelTypeIndex
		{
			get => selectedVoxelValue.GetVoxelTypeIndex();
			set => selectedVoxelValue.SetVoxelTypeIndex((byte)value);
		}

		public int VoxelTypePaletteLength => throw new NotImplementedException();

		public IReadOnlyList<UniversalVoxelBuilder> VoxelTypePaletteItems => throw new NotImplementedException();


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
				voxelFilter = GetComponent<VoxelObject>();

			if (Map == null)
				return;

			Gizmos.matrix = transform.localToWorldMatrix;

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
