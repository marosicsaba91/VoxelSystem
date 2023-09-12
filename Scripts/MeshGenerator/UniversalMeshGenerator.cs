using MUtility;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelSystem
{
	[ExecuteAlways]
	[RequireComponent(typeof(VoxelObject))]
	public class UniversalMeshGenerator : MonoBehaviour
	{
		[SerializeField] MaterialPalette materialPalette;
		[SerializeField] UniversalVoxelPalette voxelTypePalette;

		[SerializeField, HideInInspector] VoxelObject voxelFilter;

		[SerializeField] Mesh destinationMesh;
		[SerializeField] MeshFilter destinationMeshFilter;
		[SerializeField] MeshCollider destinationMeshCollider;

		[SerializeField] ChangeOn autoRegenerateMeshes = ChangeOn.Never;
		[SerializeField, Min(0)] float regenDelay;
		[SerializeField] DisplayMember regenerateMesh = new(nameof(RegenerateMeshes));
		[SerializeField] DisplayMember createMeshFile = new(nameof(CreateMeshFile));

		[SerializeField] bool doBenchmark;
		[SerializeField] TMP_Text benchmarkOutput;

		VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

		public IPalette MaterialPalette => materialPalette;
		public IPalette VoxelTypePalette => voxelTypePalette;

		public void CreateMeshFile()
		{
#if UNITY_EDITOR
			if (destinationMesh == null)
				RegenerateMeshes();

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
			if (!Application.isPlaying)
			{
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
			}
		}

		void Start()
		{
			if (voxelFilter != null)
				voxelFilter.MapChanged += OnMapChanged;
		}

		EditorCoroutine _delayedGeneration = null;

		void OnMapChanged(bool quick)
		{
			if (autoRegenerateMeshes == ChangeOn.Never) return;

			if (_delayedGeneration != null)
				EditorCoroutineUtility.StopCoroutine(_delayedGeneration);

			if ((quick && autoRegenerateMeshes == ChangeOn.OnQuickChange) ||
				autoRegenerateMeshes == ChangeOn.EveryChange)
				RegenerateMeshes();

			else if (!quick && autoRegenerateMeshes is ChangeOn.OnFinalChange)
				_delayedGeneration = EditorCoroutineUtility.StartCoroutine(RegenerateMeshesAfterDelay(), this);
		}

		IEnumerator RegenerateMeshesAfterDelay()
		{
			float time = Time.realtimeSinceStartup;
			while (Time.realtimeSinceStartup - time < regenDelay)
				yield return null;
			RegenerateMeshes();
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

		struct VoxelType
		{
			public int materialIndex;
			public int shapeIndex;
		}

		static readonly Dictionary<VoxelType, List<Vector3Int>> voxelsByType = new();

		// Runs only once.
		void BeforeMeshGeneration(VoxelMap map, UniversalVoxelPalette palette)
		{
			for (int voxelTypeIndex = 0; voxelTypeIndex < palette.VoxelTypes.Count; voxelTypeIndex++)
			{
				UniversalVoxelPaletteItem item = palette.VoxelTypes[voxelTypeIndex];
				item.BeforeMeshGeneration(map, palette, voxelTypeIndex);
			}

			ClearDictionary(materialPalette.Count, voxelTypePalette.VoxelTypes.Count);
			BuildVoxelPositionDictionary(map);
		}

		void ClearDictionary(int materialCount, int voxelTypeCount)
		{
			for (int i = 0; i < voxelTypeCount; i++)
				for (int j = 0; j < materialCount; j++)
				{
					if (voxelsByType.TryGetValue(new VoxelType() { materialIndex = j, shapeIndex = i }, out List<Vector3Int> list))
						list.Clear();
					else
						voxelsByType.Add(new VoxelType() { materialIndex = j, shapeIndex = i }, new List<Vector3Int>());
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

						int shapeIndex = voxel.GetVoxelTypeIndex();
						int materialIndex = voxel.GetMaterialIndex();

						VoxelType voxelType = new() { materialIndex = materialIndex, shapeIndex = shapeIndex };

						voxelsByType[voxelType].Add(new Vector3Int(x, y, z));
					}
		}

		// ----------------------------------------

		public void RegenerateMeshes()
		{
			if (!isActiveAndEnabled)
				return;

			if (materialPalette == null)
			{
				Debug.LogWarning("Material palette is null. Please assign a material palette to the UniversalMeshGenerator component.");
				return;
			}

			if (doBenchmark)
				benchmarkTimer ??= new BenchmarkTimer(name + " " + GetType());
			else
				benchmarkTimer = null;

			benchmarkTimer?.StartModule("Clear Lists");

			benchmarkTimer?.StartModule("Build Voxel Position Dictionary");
			BeforeMeshGeneration(Map, voxelTypePalette);

			benchmarkTimer?.StartModule("Calculate Vertex Data");
			CalculateAllVertexData();

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

		void CalculateAllVertexData()
		{
			_vertices.Clear();
			_normals.Clear();
			_uv.Clear();
			_triangles.Clear();
			_descriptors.Clear();

			int _currentTriangleIndex = 0;
			for (int materialIndex = 0; materialIndex < materialPalette.Count; materialIndex++)
			{
				for (int voxelTypeIndex = 0; voxelTypeIndex < voxelTypePalette.VoxelTypes.Count; voxelTypeIndex++)
				{
					UniversalVoxelPaletteItem item = voxelTypePalette.VoxelTypes[voxelTypeIndex];
					List<Vector3Int> voxelIndexes = voxelsByType[new VoxelType() { materialIndex = materialIndex, shapeIndex = voxelTypeIndex }];
					item.GenerateMeshData(Map, voxelIndexes, voxelTypeIndex, _vertices, _normals, _uv, _triangles);

				}
				int triangleCount = _triangles.Count - _currentTriangleIndex;
				_descriptors.Add(new SubMeshDescriptor(_currentTriangleIndex, triangleCount));
				_currentTriangleIndex = _triangles.Count;
			}
		}

		// TODO: COPY COMPONENT

		internal UniversalMeshGenerator AddACopy(GameObject newGO)
		{
			UniversalMeshGenerator generator = newGO.AddComponent<UniversalMeshGenerator>();
			generator.materialPalette = materialPalette;
			generator.voxelTypePalette = voxelTypePalette;

			generator.destinationMesh = destinationMesh;

			return generator;
		}

	}

}
