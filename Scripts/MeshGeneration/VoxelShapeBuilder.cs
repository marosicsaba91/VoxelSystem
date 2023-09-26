using MUtility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

		public virtual bool IsTransformEnabled
		{
			get => false;
			internal set { }
		}

		protected abstract bool IsSideFilled(GeneralDirection3D dir);

		public bool IsSideFilled(GeneralDirection3D dir, Flip3D flip, Vector3Int rotation)
		{
			if (!IsTransformEnabled)
				return IsSideFilled(dir);

			GeneralDirection3D transformedDir = dir.InverseTransform(flip, rotation);
			return IsSideFilled(transformedDir);
		}

		public bool IsSideFilled(GeneralDirection3D dir, int voxelValue)
		{
			if (!IsTransformEnabled)
				return IsSideFilled(dir);

			Flip3D flip = voxelValue.GetFlip();
			Vector3Int rotation = voxelValue.GetRotation();
			GeneralDirection3D transformedDir = dir.InverseTransform(flip, rotation);
			return IsSideFilled(transformedDir);
		}



		// Preview -----------------------
		 
		[SerializeField, HideInInspector] MeshBuilder previewMeshBuilder = new();
		protected Mesh previewMesh = null; 
		protected readonly List<Vector3Int> oneVoxelList = new() { Vector3Int.zero };

		public MeshBuilder GetSerializedPreviewMesh() => previewMeshBuilder;
		public Mesh GetPreviewMesh() 
		{
			if(previewMeshBuilder.VertexCount == 0)
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
			if(previewMaterial != null)
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

	}
}