using UnityEngine;
using MUtility;
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


	[RequireComponent(typeof(VoxelObject), typeof(VoxelMeshGenerator))]
	[ExecuteAlways]
	class VoxelEditor : MonoBehaviour, IVoxelEditor
	{

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

		public List<Material> MaterialPalette => meshGenerator.MaterialPalette;

		public VoxelShapePalette ShapePalette => meshGenerator.ShapePalette;

		public string MapName => voxelFilter == null ? "-" : voxelFilter.MapName;




		// Selected Tools End Editor SetupFromMesh 
		public VoxelTool SelectedTool
		{
			get => VoxelEditorSetup.SelectedTool;
			set => VoxelEditorSetup.SelectedTool = value;
		}	
		public VoxelAction SelectedAction
		{
			get => VoxelEditorSetup.SelectedAction;
			set => VoxelEditorSetup.SelectedAction = value;
		}
		public ToolState ToolState
		{
			set => VoxelEditorSetup.ToolState = value;
			get => VoxelEditorSetup.ToolState;
		} 
		public Voxel SelectedVoxelValue
		{
			get => VoxelEditorSetup.SelectedVoxelValue;
			set => VoxelEditorSetup.SelectedVoxelValue = value;
		}
		public byte SelectedMaterialIndex
		{
			get => VoxelEditorSetup.SelectedMaterialIndex;
			set => VoxelEditorSetup.SelectedMaterialIndex = value;
		}
		public int SelectedShapeId
		{
			get => VoxelEditorSetup.SelectedShapeId;
			set => VoxelEditorSetup.SelectedShapeId = value;
		}




		// --- Material Palette ---

		public Material SelectedMaterial
		{
			get
			{
				if (meshGenerator == null) return null;
				if (meshGenerator.MaterialPalette == null) return null;
				return MaterialPalette.IndexClamped(SelectedMaterialIndex);
			}
		}

// --- Shape Palette ---
		public VoxelShapeBuilder SelectedShape
		{
			get
			{
				VoxelShapePalette palette = ShapePalette;
				if (palette == null) return null;   
				return palette.GetBuilder(SelectedVoxelValue.shapeId);
			}
		}

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
