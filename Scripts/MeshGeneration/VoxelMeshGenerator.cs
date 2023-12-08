using Benchmark;
using EasyInspector;
using MUtility;
using System; 
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace VoxelSystem
{
	[ExecuteAlways]
	[RequireComponent(typeof(VoxelObject))]
	public class VoxelMeshGenerator : MonoBehaviour
	{
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

		public List<Material> MaterialPalette { get; } = new();
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
			MaterialPalette.Clear();
			if (meshRenderer != null)
				meshRenderer.GetSharedMaterials(MaterialPalette);

			//Get GUID of the shape palette
			//Get GUID of the material palette

			Guid guid = Guid.NewGuid();

		}

		void Start()
		{
			if (voxelFilter != null)
				voxelFilter.MapChanged += OnMapChanged;
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

		struct VoxelInfo : IComparable
		{
			public byte materialIndex;
			public int shapeId;

			public int CompareTo(object obj)
			{
				int compMaterial = materialIndex.CompareTo(((VoxelInfo)obj).materialIndex);
				if (compMaterial != 0) return compMaterial;
				return shapeId.CompareTo(((VoxelInfo)obj).shapeId);
			}

			public override string ToString() => $"Material: {materialIndex} Shape: {shapeId}";
		}

		static readonly SortedDictionary<VoxelInfo, List<Vector3Int>> voxelsByType = new();

		void BuildVoxelPositionDictionary(VoxelMap map)
		{
			ClearDictionary();

			Vector3Int mapSize = map.FullSize;
			for (int x = 0; x < mapSize.x; x++)
				for (int y = 0; y < mapSize.y; y++)
					for (int z = 0; z < mapSize.z; z++)
					{
						Voxel voxel = map.GetVoxel(x, y, z);

						if (voxel.IsEmpty()) continue;


						int shapeIndex = voxel.shapeId;
						byte materialIndex = voxel.materialIndex;

						VoxelInfo voxelInfo = new() { materialIndex = materialIndex, shapeId = shapeIndex };

						if (!voxelsByType.TryGetValue(voxelInfo, out List<Vector3Int> list))
						{
							list = new List<Vector3Int>();
							voxelsByType.Add(voxelInfo, list);
						}

						list.Add(new Vector3Int(x, y, z));
					}
		}

		void ClearDictionary()
		{
			List<KeyValuePair<VoxelInfo, List<Vector3Int>>> pairs = voxelsByType.Select(kvp => kvp).ToList();
			for (int i = pairs.Count - 1; i >= 0; i--)
			{
				KeyValuePair<VoxelInfo, List<Vector3Int>> kvp = pairs[i];
				VoxelInfo voxelInfo = kvp.Key;
				List<Vector3Int> coordinates = kvp.Value;

				if (coordinates.Count > 0)
					coordinates.Clear();
				else
					voxelsByType.Remove(voxelInfo);
			}
		}


		// ----------------------------------------

		public void RegenerateMeshesFull() => RegenerateMeshes(false);
		public void RegenerateMeshes(bool quick)
		{
			if (!isActiveAndEnabled)
				return;

			if (MaterialPalette == null)
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
			VoxelMap map = Map;

			// Pre-calculate VoxelData if needed
			foreach (KeyValuePair<VoxelInfo, List<Vector3Int>> chunk in voxelsByType)
			{
				if (chunk.Value.Count == 0) continue;

				int shapeId = chunk.Key.shapeId;
				VoxelShapeBuilder shapeBuilder = shapePalette.GetBuilder(shapeId);
				benchmarkTimer?.StartModule("Pre-calculate Vertex Data: " + shapeBuilder.NiceName);
				shapeBuilder.SetupVoxelData(map, chunk.Value, shapeId, quick);
			}

			// Calculate every side of every Voxel side if they are open or not
			foreach (KeyValuePair<VoxelInfo, List<Vector3Int>> chunk in voxelsByType)
			{
				if (chunk.Value.Count == 0) continue;
				int shapeId = chunk.Key.shapeId;
				VoxelShapeBuilder shapeBuilder = shapePalette.GetBuilder(shapeId);
				benchmarkTimer?.StartModule("Calculate opened/closed data for voxel sides: " + shapeBuilder.NiceName);
				List<Vector3Int> voxels = chunk.Value;
				shapeBuilder.SetupClosedSides(map, voxels, quick);
			}

			// SetupMesh for every direction
			byte lastMaterialIndex = 0; 

			foreach (KeyValuePair<VoxelInfo, List<Vector3Int>> chunk in voxelsByType)
			{
				int shapeId = chunk.Key.shapeId;
				byte materialIndex = chunk.Key.materialIndex;
				List<Vector3Int> voxelIndexes = chunk.Value;

				if (voxelIndexes.Count == 0) continue;				

				while (lastMaterialIndex < materialIndex)
				{
					meshBuilder.EndMaterialDescriptor();
					lastMaterialIndex++;
				}

				bool hasBuilder = shapePalette.TryGetBuilder(shapeId, out VoxelShapeBuilder shapeBuilder);
				if(!hasBuilder)
					Debug.LogWarning($"No shape builder found for: {chunk.Key}.    VoxelCount: {voxelIndexes.Count}");

				benchmarkTimer?.StartModule("Calculate mesh side data: " + shapeBuilder.NiceName);
				shapeBuilder.GenerateMeshData(map, voxelIndexes, shapeBuilder.VoxelId, meshBuilder, quick);
			}

			while (lastMaterialIndex < MaterialPalette.Count)
			{
				meshBuilder.EndMaterialDescriptor();
				lastMaterialIndex++;
			}
		}

		public VoxelMeshGenerator CreateACopy(GameObject go)
		{
			VoxelMeshGenerator newGen = go.AddComponent<VoxelMeshGenerator>();
			newGen.shapePalette = shapePalette;

			return newGen;
		}

		internal VoxelMeshGenerator AddACopy(GameObject newGO)
		{
			VoxelMeshGenerator generator = newGO.AddComponent<VoxelMeshGenerator>();
			generator.shapePalette = shapePalette;

			generator.destinationMesh = destinationMesh;

			return generator;
		}
	}

}
