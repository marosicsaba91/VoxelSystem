using UnityEngine;
using MUtility;
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

	[RequireComponent(typeof(VoxelObject), typeof(VoxelMeshGenerator))]
	[ExecuteAlways]
	class VoxelEditor : MonoBehaviour, IVoxelEditor
	{
		static VoxelAction selectedAction = VoxelAction.Attach;
		static VoxelTool selectedTool = VoxelTool.None;
		static ToolState toolState = ToolState.None;
		static int selectedVoxelValue = 0;

		[SerializeField, HideInInspector] internal VoxelObject voxelFilter;
		[SerializeField, HideInInspector] internal VoxelMeshGenerator meshGenerator;
		[SerializeField, HideInInspector] internal TransformLock transformLock = new();
		[SerializeField, HideInInspector] internal BoundsInt selection = new(Vector3Int.zero, Vector3Int.one * -1);

		public TransformLock TransformLock { get => transformLock; set => transformLock = value; }

		public Transform Transform => transform;

		public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

		public BoundsInt Selection { get => selection; set => selection = value; }

		public Object MapContainer => voxelFilter == null ? null :
			voxelFilter.HasSharedMap ? voxelFilter.SharedVoxelMap : voxelFilter;

		public Object EditorObject => this;

		void OnValidate()
		{
			if (voxelFilter == null)
				voxelFilter = GetComponent<VoxelObject>();

			if (meshGenerator == null)
				meshGenerator = GetComponent<VoxelMeshGenerator>();

		}

		public MaterialPalette MaterialPalette => meshGenerator.MaterialPalette;

		public VoxelShapePalette ShapePalette => meshGenerator.ShapePalette;

		public string MapName => voxelFilter == null ? "-" : voxelFilter.MapName;
		public VoxelTool SelectedTool { get => selectedTool; set => selectedTool = value; }
		public VoxelAction SelectedAction { get => selectedAction; set => selectedAction = value; }

		Flip3D selectedFlip = Flip3D.None;
		Vector3Int selectedRotation = Vector3Int.zero;

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
			set
			{

				selectedVoxelValue.SetShapeIndex((byte)value);
				if (SelectedVoxelShape.IsTransformEnabled)
				{
					selectedVoxelValue.SetFlip(selectedFlip);
					selectedVoxelValue.SetRotation(selectedRotation);
				}
				else
				{
					selectedVoxelValue.SetFlip(Flip3D.None);
					selectedVoxelValue.SetRotation(Vector3Int.zero);
				}
			}
		}
		VoxelShapeBuilder SelectedVoxelShape
		{
			get
			{
				if (meshGenerator == null) return null;
				if (meshGenerator.ShapePalette == null) return null;
				return meshGenerator.ShapePalette.Shapes[SelectedShapeIndex];
			}
		}

		public int ShapePaletteLength => throw new NotImplementedException();

		public Flip3D SelectedFlip
		{
			get => selectedVoxelValue.GetFlip();
			set
			{
				selectedFlip = value;
				if (SelectedVoxelShape.IsTransformEnabled)
					selectedVoxelValue.SetFlip(value);
			}
		}

		public Vector3Int SelectedRotation
		{
			get => selectedVoxelValue.GetRotation();
			set
			{
				selectedRotation = value;
				if (SelectedVoxelShape.IsTransformEnabled)
					selectedVoxelValue.SetRotation(value);
			}
		}

		public bool EnableVoxelTransform
		{
			get
			{
				VoxelShapeBuilder selectedVoxelShape = SelectedVoxelShape;
				if (selectedVoxelShape == null)
					return false;
				return selectedVoxelShape.IsTransformEnabled;
			}

			internal set
			{
				VoxelShapeBuilder selectedShape = SelectedVoxelShape;
				if (selectedShape != null)
					SelectedVoxelShape.IsTransformEnabled = value;
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

	static class VoxelEditorHelper 
	{
		public static bool IsEditingEnabled(this VoxelEditor editor) =>
			!Equals(editor, null) &&
			editor != null &&
			editor.enabled &&
			!Equals(editor.Map, null);
	}

}
