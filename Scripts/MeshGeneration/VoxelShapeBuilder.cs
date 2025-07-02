using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using VoxelSystem.MeshUtility;

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
		[HideInInspector] public byte previewExtraSetting;
		[HideInInspector] public CubicTransformation previewTransformation = CubicTransformation.identity;
		[HideInInspector] public VoxelShapeBuilder quickVersion;

		public readonly CustomMeshPreview meshPreview = new();

		protected static readonly Vector3 half = Vector3.one * 0.5f;

		public Material PreviewMaterial => previewMaterial;
		public int VoxelId => voxelId;

		void OnValidate()
		{
			ValidateQuickVersion();
			OnValidateInternal();
			SetupMeshPreview();
			if (voxelId == 0)
				voxelId = Random.Range(0, int.MaxValue);
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
		public abstract bool SupportsTransformation { get; }

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
			Voxel voxelValue = new(voxelId, 0, 0, previewTransformation.ToByte(), previewExtraSetting);
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

		public virtual void GetPhysicalSides(List<Vector3[]> resultSides, VoxelMap map, Vector3Int startPoint)
		{
			for (int i = 0; i < 6; i++)
			{
				GeneralDirection3D direction = DirectionUtility.generalDirection3DValues[i];
				Vector3[] localSide = sideDictionary[direction];

				Vector3[] side = new Vector3[localSide.Length];
				for (int j = 0; j < localSide.Length; j++)
					side[j] += localSide[j] + startPoint;

				resultSides.Add(side);
			}
		}

		public virtual void GetNavigationEdges(List<DirectedEdge> resultEdges, VoxelMap map, Vector3Int voxelPosition)
		{
			for (int i = 0; i < 6; i++)
			{
				GeneralDirection3D direction = DirectionUtility.generalDirection3DValues[i];
				if (map.IsFilledSafe(voxelPosition + direction.ToVectorInt())) continue;
				GetEdgesForSide(resultEdges, voxelPosition, direction);
			}
		}

		protected static void GetEdgesForSide(List<DirectedEdge> resultEdges, Vector3Int voxelPosition, GeneralDirection3D side)
		{
			GeneralDirection3D perpendicular1 = side.GetPerpendicularNext();
			GeneralDirection3D perpendicular2 = side.GetPerpendicularPrevious();

			Vector3 normal = side.ToVector();
			Vector3 p1Vector = perpendicular1.ToVector() * 0.5f;
			Vector3 p2Vector = perpendicular2.ToVector() * 0.5f;
			Vector3 center = Vector3.one * 0.5f + voxelPosition + normal * 0.5f;


			// From center to the Sides
			resultEdges.Add(new(center, center + p1Vector, normal));
			resultEdges.Add(new(center, center + p2Vector, normal));
			resultEdges.Add(new(center, center - p1Vector, normal));
			resultEdges.Add(new(center, center - p2Vector, normal));

			// From Sides to the corners
			resultEdges.Add(new(center, center + p1Vector + p2Vector, normal));
			resultEdges.Add(new(center, center + p1Vector - p2Vector, normal));
			resultEdges.Add(new(center, center - p1Vector + p2Vector, normal));
			resultEdges.Add(new(center, center - p1Vector - p2Vector, normal));

			/*
			// From corners to sides
			resultEdges.Add(new(center + p1Vector, center + p1Vector + p2Vector, normal));
			resultEdges.Add(new(center + p1Vector, center + p1Vector - p2Vector, normal));
			resultEdges.Add(new(center - p1Vector, center - p1Vector + p2Vector, normal));
			resultEdges.Add(new(center - p1Vector, center - p1Vector - p2Vector, normal));

			resultEdges.Add(new(center + p2Vector, center + p1Vector + p2Vector, normal));
			resultEdges.Add(new(center + p2Vector, center - p1Vector + p2Vector, normal));
			resultEdges.Add(new(center - p2Vector, center + p1Vector - p2Vector, normal));
			resultEdges.Add(new(center - p2Vector, center - p1Vector - p2Vector, normal));
			*/
		}


		public virtual void GetNavigationSides(List<DirectedSide> resultSides, VoxelMap map, Vector3Int voxelPosition)
		{
			for (int i = 0; i < 6; i++)
			{
				GeneralDirection3D direction = DirectionUtility.generalDirection3DValues[i];
				if (map.IsFilledSafe(voxelPosition + direction.ToVectorInt())) continue;
				resultSides.Add(GetDirectedSide(voxelPosition, direction));
			}
		}

		protected static DirectedSide GetDirectedSide(Vector3Int voxelPosition, GeneralDirection3D sideDir)
		{
			GeneralDirection3D perpendicular1 = sideDir.GetPerpendicularNext();
			GeneralDirection3D perpendicular2 = sideDir.GetPerpendicularPrevious();

			Vector3 normal = sideDir.ToVector();
			Vector3 p1Vector = perpendicular1.ToVector() * 0.5f;
			Vector3 p2Vector = perpendicular2.ToVector() * 0.5f;
			Vector3 center = Vector3.one * 0.5f + voxelPosition + normal * 0.5f;

			return new(normal,
				center + p1Vector + p2Vector,
				center - p1Vector + p2Vector,
				center - p1Vector - p2Vector,
				center + p1Vector - p2Vector);
		}
	}
}

// ---------- ExtraVoxelControl ------------------------

public abstract class ExtraVoxelControl
{
	public string name;
	public abstract Type DataType { get; }
	public abstract object GetExtraData(byte extraVoxelData);
	public abstract byte SetExtraData(byte originalExtraVoxelData, object newValue);
}

public class ExtraVoxelControl<T> : ExtraVoxelControl
{
	public delegate T GetValueDel(byte voxelData);
	public delegate byte SetValueDel(byte originalExtraVoxelData, T newValue);

	public GetValueDel getValue;
	public SetValueDel setValue;
	public sealed override object GetExtraData(byte extraVoxelData) => getValue(extraVoxelData);
	public sealed override byte SetExtraData(byte originalExtraVoxelData, object newValue) => setValue(originalExtraVoxelData, (T)newValue);
	public sealed override Type DataType => typeof(T);
}
