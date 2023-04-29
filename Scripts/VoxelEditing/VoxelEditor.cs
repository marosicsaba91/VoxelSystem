using UnityEngine;
using MUtility;
using System;
using System.Collections.Generic;

namespace VoxelSystem
{
	[Serializable]
	public struct TransformLock
	{
		public bool position;
		public bool rotation;
		public bool scale;
	}

	[RequireComponent(typeof(VoxelFilter))]
	[ExecuteAlways]
	class VoxelEditor : MonoBehaviour, IVoxelEditable
	{
		[SerializeField] VoxelFilter voxelFilter; 
		[SerializeField] TransformLock transformLock = new() { position = true, rotation = true, scale = true };

		[SerializeField] VoxelAction selectedAction = VoxelAction.Attach;
		[SerializeField] VoxelTool selectedTool = VoxelTool.None;
		[SerializeField] int selectedPaletteIndex = 0;

		public DebugReferences debugReferences;
		[Serializable]
		public struct DebugReferences
		{
			public Mesh cursorMesh;
			public Material cursorMaterial;
			public Mesh cursorVoxelMesh;
			public Material cursorVoxelMaterial;
			public bool outsideRaycast;
			public float cursorScale;
		}

		public TransformLock TransformLock { get => transformLock; set => transformLock = value; }

		public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();
		bool HasConnectedMap() => voxelFilter != null && voxelFilter.HasConnectedMap(); 

		public UnityEngine.Object RecordableUnityObject =>
			voxelFilter == null ? null :
			voxelFilter.HasConnectedMap() ? voxelFilter.ConnectedVoxelMap :
			voxelFilter;

		public bool EnableEdit => enabled;

		private void OnValidate()
		{
			voxelFilter = GetComponent<VoxelFilter>();
			selectedPaletteIndex = Mathf.Max(Mathf.Min(selectedPaletteIndex, PaletteLength - 1), 0);
		}

		private void Update()
		{
			VoxelMap map = voxelFilter.GetVoxelMap();
			if (map == null) return;

			// RAYCAST 
			var ray = new Ray(); // TODO: get ray from mouse position

			if (map.Raycast(ray, out VoxelHit hit, transform, debugReferences.outsideRaycast))
			{
				Matrix4x4 transformMatrix = transform.localToWorldMatrix;

				DrawVoxel(debugReferences, hit, transformMatrix);
				DrawCursor(debugReferences, hit, transformMatrix);

				// selection.activeObject = renderer.gameObject;
				// Debug.Log(evetType);
			}
		}


		private static void DrawVoxel(DebugReferences setup, VoxelHit hit, Matrix4x4 transformMatrix)
		{
			Mesh mesh = setup.cursorMesh;
			Material mat = setup.cursorVoxelMaterial;
			if (mesh == null) return;
			if (mat == null) return;
			var voxelMatrix = Matrix4x4.Translate(hit.voxelIndex + Vector3.one * 0.5f);
			Graphics.DrawMesh(mesh, transformMatrix * voxelMatrix, mat, 0);
		}

		private static void DrawCursor(DebugReferences setup, VoxelHit hit, Matrix4x4 transformMatrix)
		{
			Mesh mesh = setup.cursorMesh;
			Material mat = setup.cursorMaterial;
			if (mesh == null) return;
			if (mat == null) return;
			var cursorRotation = Quaternion.LookRotation(hit.side.ToVector());
			var cursorMatrix = Matrix4x4.TRS(hit.hitWorldPosition, cursorRotation, Vector3.one * setup.cursorScale);
			Graphics.DrawMesh(mesh, transformMatrix * cursorMatrix, mat, 0);
		}

		public string MapName => HasConnectedMap() ? voxelFilter.ConnectedVoxelMap.name : voxelFilter.name;
		public VoxelTool SelectedTool { get => selectedTool; set => selectedTool = value; }
		public VoxelAction SelectedAction { get => selectedAction; set => selectedAction = value; }
		public int SelectedPaletteIndex { get => selectedPaletteIndex; set => selectedPaletteIndex = value; }
		public int PaletteLength => 0;
		public IEnumerable<PaletteItem> GetPaletteItems() { yield break; }
}
}
