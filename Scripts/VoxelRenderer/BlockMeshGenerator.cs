using MUtility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelSystem;

[ExecuteAlways]
[RequireComponent(typeof(VoxelFilter))]
public class BlockMeshGenerator : MonoBehaviour
{
	[SerializeField, HideInInspector] VoxelFilter voxelFilter;
	[SerializeField, HideInInspector] MeshFilter meshFilter;
	[SerializeField] internal VoxelPalette voxelPalette;
	[SerializeField] internal bool mergeCloseEdgesOnTestMesh;
	[SerializeField] bool doBenchmark;

	[SerializeField] Mesh destinationMesh;

	[SerializeField] DisplayMember regenerateMesh = new(nameof(RegenerateMeshes));
	[SerializeField] DisplayMember createMeshFile = new(nameof(CreateMeshFile));

	public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix;
	 
	public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

	void CreateMeshFile()
	{
# if UNITY_EDITOR
		if (voxelFilter == null)
		{
			Debug.LogWarning("No voxelFilter");
		}

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


	//void LateUpdate()
	//{
	//	// RenderMesh();
	//}
	//void RenderMesh()
	//{
	//	if (voxelPalette == null)
	//		return;


	//	if(voxelPalette.Length != serializableMeshes.Count)
	//		RegenerateMeshes();


	//	int i = 0;
	//	foreach(VoxelPaletteItem paletteItem in voxelPalette.Items)
	//	{
	//		SerializableMesh sMesh = serializableMeshes[i];

	//		StartBenchmarkModul("Create Mesh from SerializableMesh" , i);
	//		Mesh mesh = sMesh.GetMesh();

	//		StartBenchmarkModul("Render Mesh" , i);
	//		Graphics.DrawMesh(mesh, transform.localToWorldMatrix, paletteItem.material, gameObject.layer);
	//		i++;
	//	}

	//	if (doBenchmark)
	//	{
	//		if(_timer.GetTotalTotalMilliseconds() > 0)
	//			Debug.Log(_timer.ToString());
	//		_timer.Clear();
	//	}
	//}


	static BenchmarkTimer _benchmarkTimer = new BenchmarkTimer();
	internal VoxelPalette VoxelPalette => voxelPalette;


	public static List<Vector3> _vertices = new();
	public static List<Vector3> _normals = new();
	public static List<Vector2> _uv = new();
	public static List<int> _triangles = new(); 
	public static int _currentTriangleIndex = 0;
	public static List<SubMeshDescriptor> _descriptors = new();

	const int vertexLimitOf16Bit = 65536;

	internal void RegenerateMeshes()
	{
		StartBenchmarkModul("Clear Lists");
		_vertices.Clear();
		_normals.Clear();
		_uv.Clear();
		_triangles.Clear();
		_currentTriangleIndex = 0;
		_descriptors.Clear();


		VoxelMap map = Map;
		int i = 0;
		foreach (VoxelPaletteItem paletteItem in voxelPalette.Items)
		{
			RegenerateMeshData(map, paletteItem, i);
			i++;
		}

		StartBenchmarkModul("Setup Mesh");
		destinationMesh.Clear();
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

		destinationMesh.UploadMeshData(true);

		if (doBenchmark)
		{
			Debug.Log(_benchmarkTimer.ToString());
			_benchmarkTimer.Clear();
		}



		if (meshFilter != null)
			meshFilter.mesh = destinationMesh;
	}
	
	void RegenerateMeshData(VoxelMap voxelMap, VoxelPaletteItem paletteItem, int index)
	{
		if (voxelMap == null) return;
		if (destinationMesh == null)
		{
			destinationMesh = new()
			{
				name = voxelFilter.MapName
			};
		}

		StartBenchmarkModul("Generate Blocks based on Map", index);
		BlockVoxelBuilder.CalculateBlocks(voxelMap, index, _blockCache, mergeCloseEdgesOnTestMesh);

		StartBenchmarkModul("Generate Vertex & Triangle data", index);
		BlockVoxelBuilder.BuildMeshFromBlocks(paletteItem.blockLibrary, _blockCache, _vertices, _normals, _uv, _triangles);
		
		StartBenchmarkModul("Generate SubMesh data", index);
		int triangleCount = _triangles.Count - _currentTriangleIndex;
		_descriptors.Add(new SubMeshDescriptor(_currentTriangleIndex, triangleCount));
		_currentTriangleIndex = _triangles.Count;
	}

	// Mesh Generation
	static readonly List<Block> _blockCache = new();


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