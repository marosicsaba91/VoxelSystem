using MUtility;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VoxelSystem
{
	public abstract class VoxelShapeBuilder : ScriptableObject, IPaletteItem
	{
		[SerializeField] string niceName;
		[SerializeField] Color color;
		[SerializeField] Material previewMaterial;
		[SerializeField] CustomMeshPreview meshPreview = new();
		[SerializeField] VoxelShapeBuilder quickVersion;

		protected void OnValidate()
		{
			ValidateQuickVersion();
			ValidateInternal();
			RecalculatePreviewMeshBuilder();

			SetupMeshPreview();

			AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
			AssemblyReloadEvents.beforeAssemblyReload += Dispose;
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
					Debug.LogWarning("Quick version can not have a quick version");
				}
				if (quickVersion == this)
				{
					quickVersion = null;
					Debug.LogWarning("Quick version can not be self");
				}
			}
		}

		protected virtual void ValidateInternal() { }

		public string DisplayName => niceName.IsNullOrEmpty() ? name : niceName;

		public Color DisplayColor => color;

		VoxelShapeBuilder GetVoxelVersion(bool quick) => (quick && quickVersion != null) ? quickVersion : this;

		internal void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder,
			bool quick)
		{
			GetVoxelVersion(quick).GenerateMeshData(map, voxelPositions, shapeIndex, meshBuilder);
		}

		protected abstract void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder);
		

		public abstract bool IsSideFilled(GeneralDirection3D dir);


		// Preview -----------------------

		[SerializeField, HideInInspector] MeshBuilder previewMeshBuilder = new();
		protected Mesh previewMesh = null;
		protected readonly List<Vector3Int> oneVoxelList = new() { Vector3Int.zero };

		public MeshBuilder GetSerializedPreviewMesh() => previewMeshBuilder;

		public Mesh GetPreviewMesh()
		{
			if (previewMeshBuilder.VertexCount == 0)
				RecalculatePreviewMeshBuilder();

			if (previewMesh == null)
			{
				previewMesh = new Mesh();
			}
			else
			{
				previewMesh.Clear();
			}

			previewMeshBuilder.CopyToMesh(previewMesh);

			return previewMesh;
		}


		void SetupMeshPreview()
		{
			meshPreview.meshGetter = GetPreviewMesh;
			meshPreview.BackgroundType = CameraClearFlags.Skybox;
			meshPreview.BackgroundColor = color;
			meshPreview.isExpandable = false;
			if (previewMaterial != null)
				meshPreview.SetMaterials(previewMaterial);
			else
				meshPreview.SetMaterials();
		}

		protected virtual void RecalculatePreviewMeshBuilder()
		{
			ArrayVoxelMap map = ArrayVoxelMap.OneVoxelMap;
			previewMeshBuilder.Clear();
			GenerateMeshData(map, oneVoxelList, 0, previewMeshBuilder, false);

			if (previewMesh == null)
				previewMesh = new Mesh();
			else
				previewMesh.Clear();

			previewMeshBuilder.CopyToMesh(previewMesh);

		}
		
		public virtual IReadOnlyList<ExtraControl> GetExtraControls() => null;

	}

	public abstract class ExtraControl
	{ 
		public string name;
		public abstract Type DataType { get; }
		public abstract object GetExtraData(ushort extraVoxelData);
		public abstract ushort SetExtraData(ushort originalExtraVoxelData, object newValue);
	}

	public class ExtraControl<T> : ExtraControl
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