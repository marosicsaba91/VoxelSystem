using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using MUtility;
using UnityEngine;
using MessageType = MUtility.MessageType;

namespace VoxelSystem
{
	enum TransformAction
	{
		None,
		OffsetX,
		OffsetY,
		OffsetZ,
		RotateX,
		RotateY,
		RotateZ,
		MirrorX,
		MirrorY,
		MirrorZ,
	}

	[Serializable]
	struct MeshInfoNew
	{
		public Mesh mesh;
		public Vector3 offset;
		public Vector3 rotation;
		public Vector3 scale;
		public SubVoxelFlags enabledSubVoxels;
		public Axis3D enabledAxis;

		public TransformAction action1;
		public TransformAction action2;
		public TransformAction action3;

	}

	[Serializable]
	struct BlockSetupNew
	{
		public bool useThisSetup;
		public BlockType blockType;
		public List<MeshInfoNew> meshInfos;
	}

	[ExecuteAlways]
	public class BlockLibrary : MonoBehaviour
	{
		// REFERENCES
		[SerializeField, HideInInspector] MeshFilter meshFilter;
		[SerializeField, HideInInspector] MeshRenderer meshRenderer;

		[SerializeField, HideInInspector] MeshFilter basicsMeshFilter;
		[SerializeField, HideInInspector] MeshRenderer basicsMeshRenderer;

		[SerializeField, HideInInspector] MeshFilter selectedMeshFilter;
		[SerializeField, HideInInspector] MeshRenderer selectedMeshRenderer;

		[SerializeField, HideInInspector] MeshFilter notSetUpMeshFilter;
		[SerializeField, HideInInspector] MeshRenderer notSetUpMeshRenderer;



		[SerializeField] BlockSetup[] blockSetups;  // LEGACY !!!!!

		// SETTINGS
		[Header("Settings")]
		[SerializeField] Color libraryColor = Color.white;
		[SerializeField] internal Material material;

		[SerializeField] BlockSetupNew corners;
		[SerializeField] BlockSetupNew edges;
		[SerializeField] BlockSetupNew sides;
		[SerializeField] BlockSetupNew negativeCorners;
		[SerializeField] BlockSetupNew negativeSides;
		[SerializeField] BlockSetupNew negativeEdges;
		[SerializeField] BlockSetupNew cross;
		[SerializeField] BlockSetupNew sideMeetsEdge;
		[SerializeField] BlockSetupNew sideMeetsNegativeEdge;
		[SerializeField] BlockSetupNew sideMeetsCorner;

		// TESTING
		[Header("Testing")]
		[SerializeField] SharedArrayVoxelMap testVoxelMap;
		[SerializeField] bool mergeCloseEdgesOnTestMesh = true;

		[Header("Generated Data")]
		// CACHED DATA - GENERATED
		// [SerializeField, ReadOnly] List<BlockKey> keys = new();
		// [SerializeField, ReadOnly] List<CustomMesh> meshes = new();
		[SerializeField] VoxelBlockLibrary voxelBlockLibrary;


		// BUTTONS
		[Header("Actions")]
		[SerializeField, UsedImplicitly] DisplayMember clearLibrary = new(nameof(Clear));
		[SerializeField, UsedImplicitly] DisplayMember regenerateLibrary = new(nameof(RegenerateLibrary));
		[SerializeField, UsedImplicitly] DisplayMember buildMesh = new(nameof(RebuildMesh));
		[SerializeField, UsedImplicitly] DisplayMember clearBlockCache = new(nameof(ClearBlockCache));

		// -------------------------------------------------------------------------------------------------------------

		public Color LibraryColor => libraryColor;

		static readonly List<Block> _blockCache = new();
		// Dictionary<BlockKey, CustomMesh> _meshCache;

		readonly BenchmarkTimer _benchmarkTimer = new("Whole Building Process");

		public BlockType selectedBlockType;
		public Axis3D selectedAxis;
		public SubVoxelFlags selectedSubVoxel;


		bool _onValidate = false;
		void OnValidate() => _onValidate = true;

		RenderParams _renderParams;


		void Update() => SafeOnValidate();

		void Clear()
		{
			if (ErrorTest())
				return;
			voxelBlockLibrary.Clear();
		}

		void SafeOnValidate()
		{
			if (!_onValidate)
				return;
			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();
			_benchmarkTimer.StartModule("Setup");
			blockSetups = GetComponentsInChildren<BlockSetup>();
			foreach (BlockSetup setup in blockSetups)
				setup.Setup();

			if (meshFilter == null)
				meshFilter = gameObject.AddComponent<MeshFilter>();
			if (meshRenderer == null)
				meshRenderer = gameObject.AddComponent<MeshRenderer>();

			meshRenderer.sharedMaterial = material;

			RebuildMesh();
			_onValidate = false;

		}

		void RebuildMesh()
		{
			if (testVoxelMap == null)
				return;
			if (meshFilter == null)
				return;

			Mesh mesh = VoxelBuilder.VoxelMapToMesh(testVoxelMap.map, GenerateMesh);
			meshFilter.sharedMesh = mesh;
		}

		void GenerateMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
		{
			if (voxelMap is null) throw new ArgumentNullException(nameof(voxelMap));
			if (vertices is null) throw new ArgumentNullException(nameof(vertices));
			if (normals is null) throw new ArgumentNullException(nameof(normals));
			if (uv is null) throw new ArgumentNullException(nameof(uv));
			if (triangles is null) throw new ArgumentNullException(nameof(triangles));
			_benchmarkTimer.StartModule("Calculating Blocks");
			if (_blockCache.IsEmpty())
				BlockVoxelBuilder.CalculateBlocks(voxelMap, _blockCache, mergeCloseEdgesOnTestMesh);

			_benchmarkTimer.StartModule("Building the Mesh");
			BlockVoxelBuilder.BuildMeshFromBlocks(voxelBlockLibrary, _blockCache, vertices, normals, uv, triangles);


			_benchmarkTimer.Stop();
			Debug.Log(_benchmarkTimer.ToString());
			_benchmarkTimer.Clear();
		}

		void ClearBlockCache() => _blockCache.Clear();

		public bool ErrorTest()
		{
			if (!EnableRegenerate())
			{
				Debug.LogWarning("Regenerating the Library is not allowed is the prefab is not loaded!");
				return true;
			}

			return false;
		}

		void RegenerateLibrary()
		{
			if (ErrorTest())
				return;

			voxelBlockLibrary.Clear();
			foreach (BlockSetup setup in blockSetups)
			{
				setup.Setup();

				BlockType blockType = setup.blockType;
				Axis3D axis = setup.axis;

				foreach (SubVoxelFlags subVoxel in SubVoxelUtility.AllSubVoxel)
				{
					Mesh mesh = setup.TryFindMesh(subVoxel);

					if (mesh == null)
						continue;

					Matrix4x4 matrix4X4 = setup.GetTransformation(subVoxel);
					CustomMesh customMesh = MeshUtility.GetTransformedMesh(mesh, matrix4X4);
					voxelBlockLibrary.AddBlock(new BlockKey(blockType, subVoxel, axis), customMesh);
				}
			}

			voxelBlockLibrary.MakeDirty();
		}

		bool EnableRegenerate() => gameObject.scene.isLoaded;




		// Gizmo Drawing -----------------------------------------------------------------------------------------------

		void OnDrawGizmos()
		{
			/*
			if (_blockCache.IsEmpty())
				BlockVoxelBuilder.CalculateBlocks(testVoxelMap.map, _blockCache, mergeCloseEdgesOnTestMesh);

			IEnumerable<Block> enumerable = _blockCache.Where(block =>
				block.blockType == selectedBlockType &&
				block.axis == selectedAxis &&
				block.inVoxelDirection == selectedSubVoxel.ToVector());

			foreach (Block block in enumerable)
			{
				Gizmos.DrawWireSphere(block.position, 0.5f);
			}
			*/
		}


		// Warning Message ---------------------------------------------------------------------------------------------


		public Mesh Mesh
		{
			get
			{
				if (meshFilter == null && !TryGetComponent(out meshFilter))
					return null;
				return meshFilter != null ? meshFilter.sharedMesh : null;
			}
		}

		public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix;
		public Material Material => material;

		// -------------------------------------------------------------------------------------------------------------
	}
}