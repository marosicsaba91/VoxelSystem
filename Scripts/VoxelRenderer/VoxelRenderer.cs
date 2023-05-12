using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelSystem;

[ExecuteAlways]
[RequireComponent(typeof(VoxelFilter))]
public class VoxelRenderer : MonoBehaviour
{
	[SerializeField, HideInInspector] VoxelFilter voxelFilter;	
	[SerializeField] internal VoxelPalette voxelPalette;
	[SerializeField] internal bool mergeCloseEdgesOnTestMesh;
	[SerializeField] bool doBenchmark;

	//[SerializeField] List<Mesh> destinationMeshes = new();
	[SerializeField] Mesh destinationMesh;

	[SerializeField] DisplayMember regenerateMesh = new(nameof(RegenerateMeshes));

	public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix;
	 
	public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

	void OnValidate()
	{
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
		int meshLength = voxelPalette.Length;

		VoxelMap map = Map;
		if (map == null) return;
		if (voxelPalette == null) return;

		int i = 0;

		StartBenchmarkModul("Clear Lists");
		_vertices.Clear();
		_normals.Clear();
		_uv.Clear();
		_triangles.Clear();
		_currentTriangleIndex = 0;
		_descriptors.Clear();


		foreach (VoxelPaletteItem paletteItem in voxelPalette.Items)
		{
			if (meshLength <= i)
			{
				Debug.LogWarning("There are not enough Mesh for all palette items");
				break;
			}

			RegenerateMesh(map, destinationMesh, paletteItem, i);
			i++;
		}

		destinationMesh.indexFormat = _vertices.Count >= vertexLimitOf16Bit ?
			IndexFormat.UInt32 : IndexFormat.UInt16;
		destinationMesh.name = "VoxelMesh";

		StartBenchmarkModul("Copy Vertex data to Mesh");
		destinationMesh.vertices = _vertices.ToArray();
		destinationMesh.normals = _normals.ToArray();
		destinationMesh.uv = _uv.ToArray();
		destinationMesh.triangles = _triangles.ToArray();

		destinationMesh.subMeshCount = _descriptors.Count;
		for (int j = 0; j < _descriptors.Count; j++)
		{
			SubMeshDescriptor descriptor = _descriptors[j];
			destinationMesh.SetSubMesh(j, descriptor);
		}

		if (doBenchmark)
		{
			Debug.Log(_benchmarkTimer.ToString());
			_benchmarkTimer.Clear();
		}
	}

	void RegenerateMesh(VoxelMap voxelMap, Mesh destinationMesh, VoxelPaletteItem paletteItem, int index)
	{
		if (voxelMap == null) return;

		StartBenchmarkModul("Generate Blocks based on Map", index);
		BlockVoxelBuilder.CalculateBlocks(voxelMap, index, _blockCache, mergeCloseEdgesOnTestMesh);
		 
		BlockVoxelBuilder.BuildMeshFromBlocks(paletteItem.blockLibrary, _blockCache, _vertices, _normals, _uv, _triangles); 
		StartBenchmarkModul("Clear Mesh data", index);
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
			_benchmarkTimer.StartModule(message + index);
	}

}