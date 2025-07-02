using EasyEditor;
using VoxelSystem.MeshUtility;
using MUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace VoxelSystem
{
	public partial class VoxelObject : MonoBehaviour
	{
		enum MeshGenerationMode
		{
			None,
			AlwaysQuick,
			AlwaysFull,
			QuickOnEditFullOnFinish,
			QuickOnFinish,
		}

		[Serializable]
		class MeshDestination
		{
			public MeshRenderer meshRenderer;
			public MeshFilter destinationMeshFilter;
			public MeshCollider destinationMeshCollider;
			public Mesh fullMesh;
			public Mesh quickMesh;
			public MeshGenerationMode generateForMeshFilter = MeshGenerationMode.QuickOnEditFullOnFinish;
			public MeshGenerationMode generateForCollider = MeshGenerationMode.QuickOnFinish;
		}

		[Header("Mesh Generation")]
		[SerializeField] VoxelShapePalette shapePalette;
		[SerializeField] bool autoRegenerateMeshes = true;

		[SerializeField] MeshDestination meshDestination = new();

		[Header("Benchmarking")]
		[SerializeField] bool doBenchmark;
		[SerializeField] TMP_Text benchmarkOutput;

		public List<Material> MaterialPalette { get; } = new();
		public VoxelShapePalette ShapePalette => shapePalette;

		[EasyDraw]
		public void CreateFullMeshFile() => CreateMeshFile(false);
		[EasyDraw]
		public void CreateQuickMeshFile() => CreateMeshFile(true);
		[EasyDraw]
		public void RegenerateMeshesFinal() => RegenerateMeshesAndUpdateMeshComponents(isFinal: true);

		public event Action<Mesh> MeshGenerated;
		public event Action<Mesh> FullMeshGenerated;
		public event Action<Mesh> QuickMeshGenerated;

		public void CreateMeshFile(bool quick)
		{
#if UNITY_EDITOR
			Mesh mesh = new();
			RegenerateMesh(quick, ref mesh);

			string path = UnityEditor.EditorUtility.SaveFilePanel(
				"Save Voxel Mesh",
				"",
				MapName + ".asset",
				"asset");

			int index = path.IndexOf("Assets/");
			if (index >= 0)
				path = path[index..];

			if (!path.IsNullOrEmpty())
				UnityEditor.AssetDatabase.CreateAsset(mesh, path);
#endif
		}

		void EditorUpdate_MeshGeneration()
		{
			MaterialPalette.Clear();
			if (meshDestination.meshRenderer != null)
				meshDestination.meshRenderer.GetSharedMaterials(MaterialPalette);
		}

		void RegenerateMeshesAndUpdateMeshComponents(bool isFinal)
		{
			if (!autoRegenerateMeshes) return;

			bool generateQuick = isFinal ?
				meshDestination.generateForMeshFilter is MeshGenerationMode.AlwaysQuick or MeshGenerationMode.QuickOnFinish ||
				meshDestination.generateForCollider is MeshGenerationMode.AlwaysQuick or MeshGenerationMode.QuickOnFinish
				:
				(meshDestination.generateForMeshFilter is MeshGenerationMode.QuickOnEditFullOnFinish ||
				meshDestination.generateForCollider is MeshGenerationMode.QuickOnEditFullOnFinish);

			if (generateQuick)
				RegenerateMesh(isQuick: true, ref meshDestination.quickMesh);

			bool generateFull = isFinal ?
				meshDestination.generateForMeshFilter is MeshGenerationMode.QuickOnEditFullOnFinish or MeshGenerationMode.AlwaysFull ||
				meshDestination.generateForCollider is MeshGenerationMode.QuickOnEditFullOnFinish or MeshGenerationMode.AlwaysFull
				:
				(meshDestination.generateForMeshFilter == MeshGenerationMode.AlwaysFull ||
				meshDestination.generateForCollider == MeshGenerationMode.AlwaysFull);


			if (generateFull)
				RegenerateMesh(isQuick: false, ref meshDestination.fullMesh);

			UpdateMeshComponents(isFinal);

			MeshGenerated?.Invoke(isFinal ? meshDestination.fullMesh : meshDestination.quickMesh);

			if (generateQuick)
				QuickMeshGenerated?.Invoke(meshDestination.quickMesh);

			if (generateFull)
				FullMeshGenerated?.Invoke(meshDestination.fullMesh);
		}

		void OnValidate()
		{
			if (meshDestination.destinationMeshFilter == null)
				meshDestination.destinationMeshFilter = GetComponent<MeshFilter>();
			if (meshDestination.destinationMeshCollider == null)
				meshDestination.destinationMeshCollider = GetComponent<MeshCollider>();
			if (meshDestination.meshRenderer == null)
				meshDestination.meshRenderer = GetComponent<MeshRenderer>();
		}

		static ModularStopwatch benchmarkTimer;

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

			public override readonly string ToString() => $"Material: {materialIndex} Shape: {shapeId}";
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

		public void RegenerateMesh(bool isQuick, ref Mesh destinationMesh)
		{
			if (MaterialPalette == null)
				Debug.LogWarning("Material palette is null. Please assign a material palette to the MeshGenerator component.");


			if (doBenchmark)
				benchmarkTimer ??= new ModularStopwatch(name + " " + GetType());
			else
				benchmarkTimer = null;

			benchmarkTimer?.StartModule("Clear Lists");
			meshBuilder.Clear();

			benchmarkTimer?.StartModule("Build Voxel Position Dictionary");
			BuildVoxelPositionDictionary(GetVoxelMap());

			CalculateAllMeshData(isQuick);

			benchmarkTimer?.StartModule("Copy Vertex data to Mesh");

			if (destinationMesh == null)
				destinationMesh = new() { name = MapName };
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
		}

		void UpdateMeshComponents(bool isFinal)
		{
			if (meshDestination.destinationMeshFilter != null &&
				TryGetMesh(meshDestination.generateForMeshFilter, isFinal, out Mesh filterMesh))
				meshDestination.destinationMeshFilter.mesh = filterMesh;

			if (meshDestination.destinationMeshCollider != null &&
				TryGetMesh(meshDestination.generateForCollider, isFinal, out Mesh colliderMesh))
				meshDestination.destinationMeshCollider.sharedMesh = colliderMesh;
		}

		bool TryGetMesh(MeshGenerationMode mode, bool isFinal, out Mesh result)
		{
			if (mode == MeshGenerationMode.QuickOnFinish && isFinal)
			{
				result = meshDestination.quickMesh;
				return true;
			}
			if (mode == MeshGenerationMode.AlwaysQuick)
			{
				result = meshDestination.quickMesh;
				return true;
			}
			if (mode == MeshGenerationMode.AlwaysFull)
			{
				result = meshDestination.fullMesh;
				return true;
			}
			if (mode == MeshGenerationMode.QuickOnEditFullOnFinish)
			{
				result = isFinal ? meshDestination.fullMesh : meshDestination.quickMesh;
				return true;
			}

			result = null;
			return false;
		}

		void CalculateAllMeshData(bool quick)
		{
			VoxelMap map = GetVoxelMap();

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
				benchmarkTimer?.StartModule("Calculate opened/closed data for voxel resultSides: " + shapeBuilder.NiceName);
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
				if (!hasBuilder)
					Debug.LogWarning($"No shape builder found for: {chunk.Key}.    VoxelCount: {voxelIndexes.Count}");

				benchmarkTimer?.StartModule("Calculate resultSides side data: " + shapeBuilder.NiceName);
				shapeBuilder.GenerateMeshData(map, voxelIndexes, shapeBuilder.VoxelId, meshBuilder, quick);
			}

			while (lastMaterialIndex < MaterialPalette.Count)
			{
				meshBuilder.EndMaterialDescriptor();
				lastMaterialIndex++;
			}
		}

		public VoxelObject CreateACopy(GameObject go)
		{
			VoxelObject newGen = go.AddComponent<VoxelObject>();
			newGen.shapePalette = shapePalette;

			return newGen;
		}

		// ------------------ PHYSICAL SHAPE GENERATION ------------------ //

		public void GeneratePhysicalMesh(List<Vector3[]> resultSides)
		{
			VoxelMap map = GetVoxelMap();
			BuildVoxelPositionDictionary(map);
			foreach (KeyValuePair<VoxelInfo, List<Vector3Int>> chunk in voxelsByType)
			{
				if (chunk.Value.Count == 0) continue;

				int shapeId = chunk.Key.shapeId;
				VoxelShapeBuilder shapeBuilder = shapePalette.GetBuilder(shapeId);

				foreach (Vector3Int voxelPosition in chunk.Value)
					shapeBuilder.GetPhysicalSides(resultSides, map, voxelPosition);
			}
		}

		public void GetNavigationEdges(List<DirectedEdge> resultEdges)
		{
			VoxelMap map = GetVoxelMap();
			BuildVoxelPositionDictionary(map);
			foreach (KeyValuePair<VoxelInfo, List<Vector3Int>> chunk in voxelsByType)
			{
				if (chunk.Value.Count == 0) continue;

				int shapeId = chunk.Key.shapeId;
				VoxelShapeBuilder shapeBuilder = shapePalette.GetBuilder(shapeId);

				foreach (Vector3Int voxelPosition in chunk.Value)
					shapeBuilder.GetNavigationEdges(resultEdges, map, voxelPosition);
			}
		}


		public void GetNavigationSides(List<DirectedSide> sides)
		{
			VoxelMap map = GetVoxelMap();
			BuildVoxelPositionDictionary(map);
			foreach (KeyValuePair<VoxelInfo, List<Vector3Int>> chunk in voxelsByType)
			{
				if (chunk.Value.Count == 0) continue;

				int shapeId = chunk.Key.shapeId;
				VoxelShapeBuilder shapeBuilder = shapePalette.GetBuilder(shapeId);

				foreach (Vector3Int voxelPosition in chunk.Value)
					shapeBuilder.GetNavigationSides(sides, map, voxelPosition);
			}
		}
	}

	public struct DirectedEdge
	{
		public Vector3 a, b, normal;
		public DirectedEdge(Vector3 a, Vector3 b, Vector3 normal)
		{
			this.a = a;
			this.b = b;
			this.normal = normal;
		}
	}

	public struct DirectedSide
	{
		public Vector3[] points;
		public Vector3 normal;

		// Constructor
		public DirectedSide(Vector3 normal, params Vector3[] points)
		{
			this.points = points;
			this.normal = normal;
		}

	}
}
