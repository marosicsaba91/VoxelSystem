using UnityEngine;
using System;
using Object = UnityEngine.Object;

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
		static int selectedVoxelValue = 0;

		[SerializeField, HideInInspector] internal VoxelObject voxelFilter;
		[SerializeField, HideInInspector] internal MeshGenerator meshGenerator;
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
				meshGenerator = GetComponent<MeshGenerator>();
		}

		public IPalette MaterialPalette => meshGenerator.MaterialPalette;

		public IPalette ShapePalette => meshGenerator.ShapePalette;

		public string MapName => voxelFilter == null ? "-" : voxelFilter.MapName;
		public VoxelTool SelectedTool { get => selectedTool; set => selectedTool = value; }
		public VoxelAction SelectedAction { get => selectedAction; set => selectedAction = value; }
		public ToolState ToolState
		{
			get => toolState;
			set => toolState = value;
		}

		public int SelectedVoxelValue
		{
			get => selectedVoxelValue;
			set => selectedVoxelValue = value;
		}

		// --- Material Palette ---
		public int SelectedMaterialIndex
		{
			get => selectedVoxelValue.GetMaterialIndex();
			set => selectedVoxelValue.SetMaterialIndex((byte)value);
		}

		public MaterialSetup SelectedMaterial
		{
			get
			{
				if (meshGenerator == null) return null;
				if (meshGenerator.MaterialPalette == null) return null;
				return meshGenerator.MaterialPalette.Materials[SelectedMaterialIndex];
			}
		}

		// --- Shape Palette ---
		public int SelectedShapeIndex
		{
			get => selectedVoxelValue.GetShapeIndex();
			set => selectedVoxelValue.SetShapeIndex((byte)value);
		}
		VoxelShape SelectedVoxelShape
		{
			get
			{
				if (meshGenerator == null) return null;
				if (meshGenerator.ShapePalette == null) return null;
				return meshGenerator.ShapePalette.Shapes[SelectedShapeIndex];
			}
		}

		public int ShapePaletteLength => throw new NotImplementedException();

		public Flip GetFlipped
		{
			get => selectedVoxelValue.GetFlip();
			internal set => selectedVoxelValue.SetFlip(value);
		}

		public Vector3Int VoxelRotation
		{
			get => selectedVoxelValue.GetRotation();
			internal set => selectedVoxelValue.SetRotation(value);
		}

		public bool EnableFlip
		{
			get
			{
				VoxelShape selectedVoxelShape = SelectedVoxelShape;
				if (selectedVoxelShape == null)
					return false;
				return selectedVoxelShape.IsFlipEnabled;
			}

			internal set
			{
				VoxelShape selectedShape = SelectedVoxelShape;
				if (selectedShape != null)
					SelectedVoxelShape.IsFlipEnabled = value;
			}
		}

		public bool EnableRotations
		{
			get
			{
				VoxelShape selectedShape = SelectedVoxelShape;
				if (selectedShape == null)
					return false;

				return SelectedVoxelShape.IsRotationEnabled;
			}

			internal set
			{
				VoxelShape selectedVoxelBuilder = SelectedVoxelShape;
				if (selectedVoxelBuilder != null)
					SelectedVoxelShape.IsRotationEnabled = value;
			}
		}

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
