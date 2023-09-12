using MUtility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelSystem
{
	[ExecuteAlways]
	[RequireComponent(typeof(VoxelObject))]
	public class UniversalMeshGenerator : VoxelMeshGenerator<UniversalVoxelPalette, UniversalVoxelPaletteItem>
	{
		[SerializeField] MaterialPalette materialPalette;

		struct VoxelType
		{
			public int materialIndex;
			public int shapeIndex;
		}

		static readonly Dictionary<VoxelType, List<Vector3Int>> voxelsByType = new();

		// Runs only once.
		protected override void BeforeMeshGeneration(VoxelMap map, UniversalVoxelPalette palette)
		{
			for (int voxelTypeIndex = 0; voxelTypeIndex < palette.ItemsList.Count; voxelTypeIndex++)
			{
				UniversalVoxelPaletteItem item = palette.ItemsList[voxelTypeIndex];
				item.BeforeMeshGeneration(map, palette, voxelTypeIndex);
			}

			ClearDictionary(materialPalette.Count, voxelPalette.ItemsList.Count);
			BuildVoxelDictionary(map);
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

		void BuildVoxelDictionary(VoxelMap map)
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

						VoxelType voxelType = new() { materialIndex = materialIndex, shapeIndex = shapeIndex };

						voxelsByType[voxelType].Add(new Vector3Int(x, y, z));
					}
		}

		// ----------------------------------------

		public override void RegenerateMeshes()
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

			_vertices.Clear();
			_normals.Clear();
			_uv.Clear();
			_triangles.Clear();
			_descriptors.Clear();

			// benchmarkTimer?.StartModule("Generate Voxel Index List by VoxelType");
			BeforeMeshGeneration(Map, voxelPalette);

			benchmarkTimer?.StartModule("Calculate Vertex Data");
			int _currentTriangleIndex = 0;
			for (int materialIndex = 0; materialIndex < materialPalette.Count; materialIndex++)
			{
				for (int voxelTypeIndex = 0; voxelTypeIndex < voxelPalette.ItemsList.Count; voxelTypeIndex++)
				{
					UniversalVoxelPaletteItem item = voxelPalette.ItemsList[voxelTypeIndex];
					List<Vector3Int> voxelIndexes = voxelsByType[new VoxelType() { materialIndex = materialIndex, shapeIndex = voxelTypeIndex }];
					item.GenerateMeshData(Map, voxelIndexes, voxelTypeIndex, _vertices, _normals, _uv, _triangles);

				}
				int triangleCount = _triangles.Count - _currentTriangleIndex;
				_descriptors.Add(new SubMeshDescriptor(_currentTriangleIndex, triangleCount));
				_currentTriangleIndex = _triangles.Count;
			}

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

		// Runs on every voxel type.
		protected override void GenerateMeshData(
			int paletteIndex,
			UniversalVoxelPaletteItem paletteItem,
			List<Vector3> vertices,
			List<Vector3> normals,
			List<Vector2> uv,
			List<int> triangles)
		{ }  // TO DELETE

		internal override VoxelMeshGenerator<UniversalVoxelPalette, UniversalVoxelPaletteItem> AddACopy(GameObject newGO)
		{
			UniversalMeshGenerator generator = newGO.AddComponent<UniversalMeshGenerator>();
			return generator;
		}
	}

}
