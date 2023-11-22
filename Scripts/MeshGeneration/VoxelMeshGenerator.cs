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
	public class VoxelMeshGenerator : MonoBehaviour
	{
		[SerializeField] MaterialPalette materialPalette;
		[SerializeField] VoxelShapePalette shapePalette;

		[SerializeField, HideInInspector] VoxelObject voxelFilter;

		[SerializeField] Mesh destinationMesh;
		[SerializeField] MeshFilter destinationMeshFilter;
		[SerializeField] MeshRenderer meshRenderer;
		[SerializeField] MeshCollider destinationMeshCollider;

		[SerializeField] bool autoRegenerateMeshes = true;
		[SerializeField] EasyMember regenerateMesh = new(nameof(RegenerateMeshesFull));
		[SerializeField] EasyMember createMeshFile = new(nameof(CreateMeshFile));

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
			if (meshRenderer == null)
				meshRenderer = GetComponent<MeshRenderer>();
		}

		static BenchmarkTimer benchmarkTimer;

		static readonly MeshBuilder meshBuilder = new();
		// static readonly List<Vector3> _vertices = new();
		// static readonly List<Vector3> _normals = new();
		// static readonly List<Vector2> _uv = new();
		// static readonly List<int> _triangles = new();
		// static readonly List<SubMeshDescriptor> _descriptors = new();

		struct VoxelInfo
		{
			public int materialIndex;
			public int shapeIndex;
		}

		static readonly Dictionary<VoxelInfo, List<Vector3Int>> voxelsByType = new();

		void BuildVoxelPositionDictionary(VoxelMap map)
		{
			ClearDictionary(materialPalette.Count, shapePalette.Shapes.Count);

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
			meshBuilder.Clear();

			benchmarkTimer?.StartModule("Build Voxel Position Dictionary");
			BuildVoxelPositionDictionary(Map);

			benchmarkTimer?.StartModule("Calculate Vertex Data");
			CalculateAllMeshData(quick);


			benchmarkTimer?.StartModule("Copy Vertex data to Mesh");

			if (destinationMesh == null)
				destinationMesh = new() { name = voxelFilter.MapName };
			else
				destinationMesh.Clear();
			meshBuilder.CopyToMesh(destinationMesh);

			if (doBenchmark)
			{
				string benchmarkResult = benchmarkTimer.ToString();
				if (benchmarkOutput != null)
					benchmarkOutput.text = benchmarkResult;
				Debug.Log(benchmarkResult);

				benchmarkTimer.Clear();
			}

			if (destinationMeshCollider != null && destinationMesh.vertices.Length > 0)
				destinationMeshCollider.sharedMesh = destinationMesh;
			if (destinationMeshFilter != null)
				destinationMeshFilter.sharedMesh = destinationMesh;
		}

		void CalculateAllMeshData(bool quick)
		{
			int _currentTriangleIndex = 0;
			for (int materialIndex = 0; materialIndex < materialPalette.Count; materialIndex++)
			{
				for (int shapeIndex = 0; shapeIndex < shapePalette.Shapes.Count; shapeIndex++)
				{
					VoxelShapeBuilder item = shapePalette.Shapes[shapeIndex];
					List<Vector3Int> voxelIndexes = voxelsByType[new VoxelInfo() { materialIndex = materialIndex, shapeIndex = shapeIndex }];
					item.GenerateMeshData(Map, voxelIndexes, shapeIndex, meshBuilder, quick);

				}
				// Make a function for this:
				int triangleCount = meshBuilder.TriangleCount - _currentTriangleIndex;
				meshBuilder.descriptors.Add(new SubMeshDescriptor(_currentTriangleIndex, triangleCount));
				_currentTriangleIndex = meshBuilder.TriangleCount;
			}
		}

		public VoxelMeshGenerator CreateACopy(GameObject go)
		{
			VoxelMeshGenerator newGen = go.AddComponent<VoxelMeshGenerator>();
			newGen.materialPalette = materialPalette;
			newGen.shapePalette = shapePalette;

			return newGen;
		}

		internal VoxelMeshGenerator AddACopy(GameObject newGO)
		{
			VoxelMeshGenerator generator = newGO.AddComponent<VoxelMeshGenerator>();
			generator.materialPalette = materialPalette;
			generator.shapePalette = shapePalette;

			generator.destinationMesh = destinationMesh;

			return generator;
		}

	}

}
