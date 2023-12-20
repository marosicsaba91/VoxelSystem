using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using MeshUtility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelSystem
{
	public abstract class VoxelShapeBuilder : ScriptableObject
	{
		[SerializeField] int voxelId;
		[SerializeField] string niceName;
		[Space]
		[HideInInspector] public Material previewMaterial;
		[HideInInspector] public int previewExtraSetting;
		[HideInInspector] public VoxelShapeBuilder quickVersion;

		public static readonly CustomMeshPreview meshPreview = new();

		protected static readonly Vector3 half = Vector3.one * 0.5f;

		public Material PreviewMaterial => previewMaterial;
		public int PreviewExtraSetting => previewExtraSetting;

		public int VoxelId => voxelId;


		protected void OnValidate()
		{
			ValidateQuickVersion();
			OnValidateInternal();
			SetupMeshPreview();

#if UNITY_EDITOR
			AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
			AssemblyReloadEvents.beforeAssemblyReload += Dispose;
#endif

			if (voxelId == 0)
				voxelId = Random.Range(0, int.MaxValue);
		}

		void Dispose()
		{
			if (previewMesh != null)
			{
				DestroyImmediate(previewMesh);
				previewMesh = null;
			}

			meshPreview.Dispose();
		}

		void ValidateQuickVersion()
		{
			if (quickVersion != null)
			{
				if (quickVersion.quickVersion != null)
				{
					quickVersion.quickVersion = null;
					Debug.LogWarning("AlwaysQuick version can not have a quick version");
				}
				if (quickVersion == this)
				{
					quickVersion = null;
					Debug.LogWarning("AlwaysQuick version can not be self");
				}
			}
		}

		public void InitializeAndSetupPreview()
		{
			InitializeMeshCache();
			RecalculatePreviewMesh();

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		protected abstract void InitializeMeshCache();

		protected virtual void OnValidateInternal() { }

		public string NiceName
		{
			get => niceName.IsNullOrEmpty() ? name : niceName;
			set => niceName = value;
		}

		VoxelShapeBuilder GetVoxelVersion(bool quick) => (quick && quickVersion != null) ? quickVersion : this;

		internal void SetupVoxelData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeId,
			bool quick)
		{
			GetVoxelVersion(quick).SetupVoxelData(map, voxelPositions, shapeId);
		}

		protected virtual void SetupVoxelData(VoxelMap map, List<Vector3Int> voxelPositions, int shapeIndex) { }

		internal void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions, bool quick) =>
			GetVoxelVersion(quick).SetupClosedSides(map, voxelPositions);

		protected abstract void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions);

		internal void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeId,
			MeshBuilder meshBuilder,
			bool quick)
		{
			if (!IsInitialized)
			{
				Debug.LogWarning($"Mesh Can not built. The ShapeBuilder is NOT initialized yet: {name}", this);
				return;
			}

			GetVoxelVersion(quick).GenerateMeshData(map, voxelPositions, shapeId, meshBuilder);
		}

		protected abstract bool IsInitialized { get; }

		protected abstract void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder);

		// ---------- Preview -----------------------

		[SerializeField, HideInInspector] MeshBuilder previewMeshBuilder = new();
		protected Mesh previewMesh = null;
		protected readonly List<Vector3Int> oneVoxelList = new() { Vector3Int.one };

		public MeshBuilder GetSerializedPreviewMesh() => previewMeshBuilder;

		void SetupMeshPreview()
		{
			meshPreview.meshGetter = GetPreviewMesh;
			meshPreview.BackgroundType = CameraClearFlags.Skybox;
			meshPreview.isExpandable = false;
			if (previewMaterial != null)
				meshPreview.SetMaterials(previewMaterial);
			else
				meshPreview.SetMaterials();
		}

		public Mesh GetPreviewMesh()
		{
			if (previewMesh == null)
			{
				previewMesh = new Mesh();
				previewMeshBuilder?.CopyToMesh(previewMesh);
			}
			return previewMesh;
		}

		protected virtual void RecalculatePreviewMesh()
		{
			Voxel voxelValue = new(voxelId, 0, (ushort)previewExtraSetting, 0);
			ArrayVoxelMap map = ArrayVoxelMap.GetTestOneVoxelMap(voxelValue);

			previewMeshBuilder.Clear();
			GenerateMeshData(map, oneVoxelList, voxelId, previewMeshBuilder, false);

			if (previewMesh == null)
				previewMesh = new Mesh();
			else
				previewMesh.Clear();

			previewMeshBuilder.CopyToMesh(previewMesh);
		}

		public virtual IReadOnlyList<ExtraVoxelControl> GetExtraControls() => null;



		// ---------- PhysicalVoxelShape ------------------------
		protected abstract PhysicalVoxelShape PhysicalShape(ushort extraData);

		public virtual void AddMeshSides(FlexibleMesh flexMesh, Vector3Int startPoint, ushort extraData)
		{
			Vector3 p000 = new Vector3(0, 0, 0) + startPoint;
			Vector3 p001 = new Vector3(0, 0, 1) + startPoint;
			Vector3 p010 = new Vector3(0, 1, 0) + startPoint;
			Vector3 p011 = new Vector3(0, 1, 1) + startPoint;
			Vector3 p100 = new Vector3(1, 0, 0) + startPoint;
			Vector3 p101 = new Vector3(1, 0, 1) + startPoint;
			Vector3 p110 = new Vector3(1, 1, 0) + startPoint;
			Vector3 p111 = new Vector3(1, 1, 1) + startPoint;

			flexMesh.AddFace(p000, p010, p110, p100);
			flexMesh.AddFace(p000, p100, p101, p001);
			flexMesh.AddFace(p000, p001, p011, p010);
			flexMesh.AddFace(p100, p110, p111, p101);
			flexMesh.AddFace(p010, p011, p111, p110);
			flexMesh.AddFace(p001, p101, p111, p011);
		}
	}

	// ---------- ExtraVoxelControl ------------------------

	public abstract class ExtraVoxelControl
	{
		public string name;
		public abstract Type DataType { get; }
		public abstract object GetExtraData(ushort extraVoxelData);
		public abstract ushort SetExtraData(ushort originalExtraVoxelData, object newValue);
	}

	public class ExtraVoxelControl<T> : ExtraVoxelControl
	{
		public delegate T GetValueDel(ushort voxelData);
		public delegate ushort SetValueDel(ushort originalExtraVoxelData, T newValue);

		public GetValueDel getValue;
		public SetValueDel setValue;
		public sealed override object GetExtraData(ushort extraVoxelData) => getValue(extraVoxelData);
		public sealed override ushort SetExtraData(ushort originalExtraVoxelData, object newValue) => setValue(originalExtraVoxelData, (T)newValue);
		public sealed override Type DataType => typeof(T);
	}
}

