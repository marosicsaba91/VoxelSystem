using MUtility;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelSystem
{
	[ExecuteAlways]
	[RequireComponent(typeof(VoxelObject))]
	public class MeshGenerator : MonoBehaviour
	{
		[SerializeField] MaterialPalette materialPalette;
		[SerializeField] VoxelShapePalette shapePalette;

		[SerializeField, HideInInspector] VoxelObject voxelFilter;

		[SerializeField] Mesh destinationMesh;
		[SerializeField] MeshFilter destinationMeshFilter;
		[SerializeField] MeshRenderer meshRenderer;
		[SerializeField] MeshCollider destinationMeshCollider;

		[SerializeField] bool autoRegenerateMeshes = true;
		[SerializeField] DisplayMember regenerateMesh = new(nameof(RegenerateMeshesFull));
		[SerializeField] DisplayMember createMeshFile = new(nameof(CreateMeshFile));

		[SerializeField] bool doBenchmark;
		[SerializeField] TMP_Text benchmarkOutput;

		VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

		public MaterialPalette MaterialPalette => materialPalette;
		public VoxelShapePalette ShapePalette => shapePalette;

		public void CreateMeshFile()
		{
#if UNITY_EDITOR
			if (destinationMesh == null)
				RegenerateMeshesFull();

			string path = UnityEditor.EditorUtility.SaveFilePanel(
				"Save Voxel Mesh",
				"",
				voxelFilter.MapName + ".asset",
				"asset");

			int index = path.IndexOf("Assets/");
			if (index >= 0)
				path = path[index..];

			if (!path.IsNullOrEmpty())
				UnityEditor.AssetDatabase.CreateAsset(destinationMesh, path);
#endif
		}

		VoxelObject _lastFilter;
		void Update()
		{
			if (Application.isPlaying) return;

			if (voxelFilter != null)
			{
				voxelFilter.MapChanged -= OnMapChanged;
				voxelFilter.MapChanged += OnMapChanged;
				_lastFilter = voxelFilter;
			}
			else if (_lastFilter != null)
			{
				_lastFilter.MapChanged -= OnMapChanged;
			}

			FreshRendererMaterialPalette();

			//Get GUID of the shape palette

			//Get GUID of the material palette

			Guid guid = Guid.NewGuid();
		}

		void Start()
		{
			if (voxelFilter != null)
				voxelFilter.MapChanged += OnMapChanged;
		}

		void FreshRendererMaterialPalette()
		{
			if (meshRenderer == null || materialPalette == null) return;

			List<Material> materials = new();

			foreach (MaterialSetup materialSetup in materialPalette.Materials)
				materials.Add(materialSetup.Material);

			meshRenderer.SetMaterials(materials);
		}


		void OnMapChanged(bool quick)
		{
			if (autoRegenerateMeshes)
				RegenerateMeshes(quick);
		}

		void OnValidate()
		{
			voxelFilter = GetComponent<VoxelObject>();
			if (destinationMeshFilter == null)
				destinationMeshFilter = GetComponent<MeshFilter>();
			if (destinationMeshCollider == null)
				destinationMeshCollider = GetComponent<MeshCollider>();
		}

		static BenchmarkTimer benchmarkTimer;

		static readonly List<Vector3> _vertices = new();
		static readonly List<Vector3> _normals = new();
		static readonly List<Vector2> _uv = new();
		static readonly List<int> _triangles = new();
		static readonly List<SubMeshDescriptor> _descriptors = new();

		const int vertexLimitOf16Bit = 65536;

		struct VoxelInfo
		{
			public int materialIndex;
			public int shapeIndex;
		}

		static readonly Dictionary<VoxelInfo, List<Vector3Int>> voxelsByType = new();




		// Runs only once.
		void BeforeMeshGeneration(VoxelMap map, VoxelShapePalette palette, bool quick)
		{
			for (int shapeIndex = 0; shapeIndex < palette.Shapes.Count; shapeIndex++)
			{
				VoxelShape item = palette.Shapes[shapeIndex];
				item.BeforeMeshGeneration(map, palette, shapeIndex, quick);
			}

			ClearDictionary(materialPalette.Count, shapePalette.Shapes.Count);
			BuildVoxelPositionDictionary(map);
		}

		void ClearDictionary(int materialCount, int shapeCount)
		{
			for (int i = 0; i < shapeCount; i++)
				for (int j = 0; j < materialCount; j++)
				{
					if (voxelsByType.TryGetValue(new VoxelInfo() { materialIndex = j, shapeIndex = i }, out List<Vector3Int> list))
						list.Clear();
					else
						voxelsByType.Add(new VoxelInfo() { materialIndex = j, shapeIndex = i }, new List<Vector3Int>());
				}
		}

		void BuildVoxelPositionDictionary(VoxelMap map)
		{
			Vector3Int mapSize = map.FullSize;
			for (int x = 0; x < mapSize.x; x++)
				for (int y = 0; y < mapSize.y; y++)
					for (int z = 0; z < mapSize.z; z++)
					{
						int voxel = map.GetVoxel(x, y, z);

						if (voxel.IsEmpty()) continue;

						int shapeIndex = voxel.GetShapeIndex();
						int materialIndex = voxel.GetMaterialIndex();

						VoxelInfo voxelInfo = new() { materialIndex = materialIndex, shapeIndex = shapeIndex };

						voxelsByType[voxelInfo].Add(new Vector3Int(x, y, z));
					}
		}

		// ----------------------------------------

		public void RegenerateMeshesFull() => RegenerateMeshes(false);
		public void RegenerateMeshes(bool quick)
		{
			if (!isActiveAndEnabled)
				return;

			if (materialPalette == null)
			{
				Debug.LogWarning("Material palette is null. Please assign a material palette to the MeshGenerator component.");
				return;
			}

			if (doBenchmark)
				benchmarkTimer ??= new BenchmarkTimer(name + " " + GetType());
			else
				benchmarkTimer = null;

			benchmarkTimer?.StartModule("Clear Lists");

			benchmarkTimer?.StartModule("Build Voxel Position Dictionary");
			BeforeMeshGeneration(Map, shapePalette, quick);

			benchmarkTimer?.StartModule("Calculate Vertex Data");
			CalculateAllVertexData(quick);

			if (destinationMesh == null)
				destinationMesh = new() { name = voxelFilter.MapName };

			benchmarkTimer?.StartModule("Setup Mesh");
			destinationMesh.Clear();
			destinationMesh.indexFormat = _vertices.Count >= vertexLimitOf16Bit ?
				IndexFormat.UInt32 : IndexFormat.UInt16;

			benchmarkTimer?.StartModule("Copy Vertex data to Mesh");
			destinationMesh.vertices = _vertices.ToArray();
			destinationMesh.normals = _normals.ToArray();
			destinationMesh.uv = _uv.ToArray();

			benchmarkTimer?.StartModule("Copy Triangle data to Mesh");
			destinationMesh.triangles = _triangles.ToArray();

			benchmarkTimer?.StartModule("Copy SubMesh data");
			destinationMesh.subMeshCount = _descriptors.Count;
			for (int j = 0; j < _descriptors.Count; j++)
			{
				SubMeshDescriptor descriptor = _descriptors[j];
				destinationMesh.SetSubMesh(j, descriptor);
			}

			if (doBenchmark)
			{
				string benchmarkResult = benchmarkTimer.ToString();
				if (benchmarkOutput != null)
					benchmarkOutput.text = benchmarkResult;
				Debug.Log(benchmarkResult);

				benchmarkTimer.Clear();
			}

			if (destinationMeshCollider != null)
				destinationMeshCollider.sharedMesh = destinationMesh;
			if (destinationMeshFilter != null)
				destinationMeshFilter.sharedMesh = destinationMesh;
		}

		void CalculateAllVertexData(bool quick)
		{
			_vertices.Clear();
			_normals.Clear();
			_uv.Clear();
			_triangles.Clear();
			_descriptors.Clear();

			int _currentTriangleIndex = 0;
			for (int materialIndex = 0; materialIndex < materialPalette.Count; materialIndex++)
			{
				for (int shapeIndex = 0; shapeIndex < shapePalette.Shapes.Count; shapeIndex++)
				{
					VoxelShape item = shapePalette.Shapes[shapeIndex];
					List<Vector3Int> voxelIndexes = voxelsByType[new VoxelInfo() { materialIndex = materialIndex, shapeIndex = shapeIndex }];
					item.GenerateMeshData(Map, voxelIndexes, shapeIndex, _vertices, _normals, _uv, _triangles, quick);

				}
				int triangleCount = _triangles.Count - _currentTriangleIndex;
				_descriptors.Add(new SubMeshDescriptor(_currentTriangleIndex, triangleCount));
				_currentTriangleIndex = _triangles.Count;
			}
		}

		public MeshGenerator CreateACopy(GameObject go) 
		{
			MeshGenerator newGen = go.AddComponent<MeshGenerator>();
			newGen.materialPalette = materialPalette;
			newGen.shapePalette = shapePalette;

			return newGen;
		}

		internal MeshGenerator AddACopy(GameObject newGO)
		{
			MeshGenerator generator = newGO.AddComponent<MeshGenerator>();
			generator.materialPalette = materialPalette;
			generator.shapePalette = shapePalette;

			generator.destinationMesh = destinationMesh;

			return generator;
		}

	}

}
