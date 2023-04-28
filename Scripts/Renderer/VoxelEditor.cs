using UnityEngine;
using MUtility;
using System;

namespace VoxelSystem
{
	[RequireComponent(typeof(VoxelFilter))]
	[ExecuteAlways]
	class VoxelEditor : MonoBehaviour, IVoxelEditable
	{
		[SerializeField] VoxelFilter voxelFilter;
		[SerializeField] VoxelRenderer voxelRenderer; 
		[SerializeField] bool enableEdit;

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

		TransformLocks transformLocks;

		public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();
		public void CopyMapFrom(VoxelMap source) => Map?.SetupFrom(source);
		public bool HasConnectedMap() => voxelFilter != null && voxelFilter.HasConnectedMap();
		public void RegenerateMesh() => throw new NotImplementedException();

		public ScriptableObject SharedMap =>
			voxelFilter != null && voxelFilter.HasConnectedMap() ? voxelFilter.connectedMapHolder : null;

		public UnityEngine.Object Object =>
			voxelFilter == null ? null :
			voxelFilter.HasConnectedMap() ? voxelFilter.ConnectedVoxelMap : 
			voxelFilter;

		public IVoxelBuilder Builder => voxelRenderer;

		IVoxelBuilder IVoxelEditable.Builder { get => voxelRenderer; }
		public bool LockPosition
		{
			get => transformLocks.lockPosition;
			set => transformLocks.lockPosition = value;
		}

		public bool LockRotation
		{
			get => transformLocks.lockRotation;
			set => transformLocks.lockRotation = value;
		}
		public bool LockScale
		{
			get => transformLocks.lockScale;
			set => transformLocks.lockScale = value;
		}

		[Serializable]
		public struct TransformLocks
		{
			public bool lockPosition;
			public bool lockRotation;
			public bool lockScale;
		}


		private void OnValidate()
		{
			voxelFilter = GetComponent<VoxelFilter>();
		}

		private void Update()
		{
			if (!enableEdit) return;

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


		public void ApplyRotation()
		{
			if (!transformLocks.lockRotation)
			{ return; }
			VoxelMap map = voxelFilter.GetVoxelMap();
			if (map == null)
			{ return; }
			if (transform.localRotation == Quaternion.identity)
			{ return; }


			Vector3 transformedOne = transform.TransformDirection(Vector3.one);
			Vector3 transformedSize =
				transform.TransformDirection(Vector3.right).normalized * map.FullSize.x +
				transform.TransformDirection(Vector3.up).normalized * map.FullSize.y +
				transform.TransformDirection(Vector3.forward).normalized * map.FullSize.z;
			Vector3 step = (Vector3.one - transformedOne) / 2f;
			Vector3 move = step.MultiplyAllAxis(transformedSize);
			transform.localPosition += move;


			int actionCount = 0; // For safety
			Vector3Int rotated;
			do
			{
				Vector3 localRotation = transform.localRotation.eulerAngles;
				rotated = new Vector3Int(
					Mathf.RoundToInt((localRotation.x % 360) / 90f),
					Mathf.RoundToInt((localRotation.y % 360) / 90f),
					Mathf.RoundToInt((localRotation.z % 360) / 90f));

				if (rotated.x != 0)
				{
					map.Turn(Axis3D.X, leftHandPositive: false);
					transform.Rotate(Vector3.right, angle: -90);
					actionCount++;
					continue;
				}
				if (rotated.y != 0)
				{
					map.Turn(Axis3D.Y, leftHandPositive: false);
					transform.Rotate(Vector3.up, angle: -90);
					actionCount++;
					continue;
				}
				if (rotated.z != 0)
				{
					map.Turn(Axis3D.Z, leftHandPositive: false);
					transform.Rotate(Vector3.forward, angle: -90);
					actionCount++;
				}
			}
			while (rotated != Vector3Int.zero && actionCount < 100);

		}

		public void ApplyScale()
		{
			if (!transformLocks.lockScale)
			{ return; }
			VoxelMap map = voxelFilter.GetVoxelMap();
			if (map == null)
			{ return; }
			if (transform.localScale == Vector3.one)
			{ return; }

			Vector3 move = Vector3.zero;


			move += ApplyScaleOnAxis(map, Axis3D.X);
			move += ApplyScaleOnAxis(map, Axis3D.Y);
			move += ApplyScaleOnAxis(map, Axis3D.Z);
			transform.localScale = Vector3.one;
			transform.position += transform.TransformVector(move);
		}

		private Vector3 ApplyScaleOnAxis(VoxelMap map, Axis3D axis)
		{
			Transform trans = transform;
			Vector3 localScale = trans.localScale;
			float scaleFloat =
				axis == Axis3D.X ? localScale.x :
				axis == Axis3D.Y ? localScale.y :
				localScale.z;
			return map.FullSize;
		}
	}
}
