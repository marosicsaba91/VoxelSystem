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
		[HideInInspector] public ushort previewExtraSetting;
		[HideInInspector] public VoxelShapeBuilder quickVersion;

		public readonly CustomMeshPreview meshPreview = new();

		protected static readonly Vector3 half = Vector3.one * 0.5f;

		public Material PreviewMaterial => previewMaterial;
		public ushort PreviewExtraSetting => previewExtraSetting;

		public int VoxelId => voxelId;


		protected void OnValidate()
		{
			ValidateQuickVersion();
			OnValidateInternal();
			SetupMeshPreview();
			/*
#if UNITY_EDITOR
			AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
			AssemblyReloadEvents.beforeAssemblyReload += Dispose;
#endif
			*/
			if (voxelId == 0)
				voxelId = Random.Range(0, int.MaxValue);
		}

		/*
		void Dispose()
		{
			if (previewMesh != null)
			{
				DestroyImmediate(previewMesh);
				previewMesh = null;
			}

			meshPreview.Dispose();
		}
		*/

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

		public void InitializeMeshCacheAndSave()
		{
			InitializeCachedData();

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		protected abstract void InitializeCachedData();

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

		public void RecalculatePreviewMesh()
		{
			Voxel voxelValue = new(voxelId, 0, previewExtraSetting, 0);
			ArrayVoxelMap map = ArrayVoxelMap.GetTestOneVoxelMap(voxelValue);

			previewMeshBuilder.Clear();
			GenerateMeshData(map, oneVoxelList, voxelId, previewMeshBuilder, false);

			if (previewMesh == null)
				previewMesh = new Mesh();
			else
				previewMesh.Clear();

			previewMeshBuilder.CopyToMesh(previewMesh);

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif

		}

		public virtual IReadOnlyList<ExtraVoxelControl> GetExtraControls() => null;



		// ---------- PhysicalVoxelShape ------------------------
		static readonly Vector3 p000 = new(0, 0, 0);
		static readonly Vector3 p001 = new(0, 0, 1);
		static readonly Vector3 p010 = new(0, 1, 0);
		static readonly Vector3 p011 = new(0, 1, 1);
		static readonly Vector3 p100 = new(1, 0, 0);
		static readonly Vector3 p101 = new(1, 0, 1);
		static readonly Vector3 p110 = new(1, 1, 0);
		static readonly Vector3 p111 = new(1, 1, 1);
		static readonly Dictionary<GeneralDirection3D, Vector3[]> sideDictionary = new()
		{
			{ GeneralDirection3D.Right, new Vector3[]{ p100, p110, p111, p101 } },
			{ GeneralDirection3D.Left, new Vector3[]{ p000, p001, p011, p010 } },

			{ GeneralDirection3D.Up, new Vector3[]{ p010, p011, p111, p110 } },
			{ GeneralDirection3D.Down, new Vector3[]{ p000, p100, p101, p001 } },
			{ GeneralDirection3D.Forward, new Vector3[]{ p001, p101, p111, p011 } },
			{ GeneralDirection3D.Back, new Vector3[]{ p000, p010, p110, p100 } },
		};

		public virtual void BuildPhysicalMeshSides(FlexibleMesh flexMesh, VoxelMap map, Vector3Int startPoint, ref int sideCounter)
		{
			for (int i = 0; i < 6; i++)
			{
				GeneralDirection3D direction = DirectionUtility.generalDirection3DValues[i];
				flexMesh.AddFace(sideDictionary[direction], startPoint);
				sideCounter++;
			}
		}
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


