using MUtility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelSystem;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(VoxelFilter))]
public class BlockMeshGenerator : MonoBehaviour
{
	[SerializeField, HideInInspector] VoxelFilter voxelFilter;
	[SerializeField, HideInInspector] MeshFilter meshFilter;
	[SerializeField] internal VoxelPalette voxelPalette;
	[SerializeField] internal bool mergeCloseEdgesOnTestMesh;

	[SerializeField] Mesh destinationMesh;

	[SerializeField] DisplayMember regenerateMesh = new(nameof(RegenerateMeshes));
	[SerializeField] DisplayMember createMeshFile = new(nameof(CreateMeshFile));

	[SerializeField] bool doBenchmark;
	[SerializeField] TMP_Text debugText;

	public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix;
	 
	public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

	void CreateMeshFile()
	{
# if UNITY_EDITOR 
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
		 
		if(!path.IsNullOrEmpty())
			UnityEditor.AssetDatabase.CreateAsset(destinationMesh, path);
# endif
	}



	void OnValidate()
	{
		meshFilter = GetComponent<MeshFilter>();
		voxelFilter = GetComponent<VoxelFilter>();
	}

	static BenchmarkTimer _benchmarkTimer = new BenchmarkTimer();
	internal VoxelPalette VoxelPalette => voxelPalette;


	public static List<Vector3> _vertices = new();
	public static List<Vector3> _normals = new();
	public static List<Vector2> _uv = new();
	public static List<int> _triangles = new(); 
	public static int _currentTriangleIndex = 0;
	public static List<SubMeshDescriptor> _descriptors = new();

	const int vertexLimitOf16Bit = 65536;

	public void RegenerateMeshes()
	{
		StartBenchmarkModul("Clear Lists");
		_vertices.Clear();
		_normals.Clear();
		_uv.Clear();
		_triangles.Clear();
		_currentTriangleIndex = 0;
		_descriptors.Clear();


		StartBenchmarkModul("Generate Blocks based on VoxelMap");
		GenerateBlocks(Map);

		int i = 0;
		foreach (VoxelPaletteItem paletteItem in voxelPalette.Items)
		{
			RegenerateMeshData(_blockCache[i], paletteItem, i);
			i++;
		}

		StartBenchmarkModul("Setup Mesh");
		destinationMesh.indexFormat = _vertices.Count >= vertexLimitOf16Bit ?
			IndexFormat.UInt32 : IndexFormat.UInt16;

		StartBenchmarkModul("Copy Vertex data to Mesh");
		destinationMesh.vertices = _vertices.ToArray();
		destinationMesh.normals = _normals.ToArray();
		destinationMesh.uv = _uv.ToArray();

		StartBenchmarkModul("Copy Triangle data to Mesh");
		destinationMesh.triangles = _triangles.ToArray();

		StartBenchmarkModul("Copy SubMesh data");
		destinationMesh.subMeshCount = _descriptors.Count;
		for (int j = 0; j < _descriptors.Count; j++)
		{
			SubMeshDescriptor descriptor = _descriptors[j];
			destinationMesh.SetSubMesh(j, descriptor);
		}

		// destinationMesh.UploadMeshData(true);

		if (doBenchmark)
		{
			string benchmarkResult = _benchmarkTimer.ToString();
			if (debugText != null)
				debugText.text = benchmarkResult;
			Debug.Log(benchmarkResult);

			_benchmarkTimer.Clear();
		}

		if (meshFilter != null)
			meshFilter.mesh = destinationMesh;
	}

	static readonly List<List<Block>> _blockCache = new();
	void GenerateBlocks(VoxelMap voxelMap)
	{
		if (voxelMap == null) return;

		while (_blockCache.Count > voxelPalette.Length)
			_blockCache.RemoveAt(_blockCache.Count-1);
		while (_blockCache.Count < voxelPalette.Length)
			_blockCache.Add(new List<Block>());

		foreach(List<Block> block in _blockCache)
			block.Clear();

		BlockVoxelBuilder.CalculateBlocks(voxelMap, _blockCache, mergeCloseEdgesOnTestMesh);
	}
	
	void RegenerateMeshData(List<Block> blocks, VoxelPaletteItem paletteItem, int index)
	{
		if (destinationMesh == null)
		{
			destinationMesh = new()
			{
				name = voxelFilter.MapName
			};
		}

		StartBenchmarkModul("Generate Vertex & Triangle data", index);
		BlockVoxelBuilder.BuildMeshFromBlocks(paletteItem.blockLibrary, blocks, _vertices, _normals, _uv, _triangles);
		
		StartBenchmarkModul("Generate SubMesh data", index);
		int triangleCount = _triangles.Count - _currentTriangleIndex;
		_descriptors.Add(new SubMeshDescriptor(_currentTriangleIndex, triangleCount));
		_currentTriangleIndex = _triangles.Count;
	}

	// Mesh Generation


	void StartBenchmarkModul(string message)
	{
		if (doBenchmark)
			_benchmarkTimer.StartModule(message);
	}
	void StartBenchmarkModul(string message, int index)
	{
		if (doBenchmark)
			_benchmarkTimer.StartModule(message + " - " + index);
	}

}