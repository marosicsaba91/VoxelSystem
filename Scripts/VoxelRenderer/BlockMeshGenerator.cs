using MUtility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelSystem;
using TMPro;
using UnityEngine.UIElements;
using System.Linq;

[ExecuteAlways]
[RequireComponent(typeof(VoxelFilter))]
public class BlockMeshGenerator : MonoBehaviour
{
	[SerializeField, HideInInspector] VoxelFilter voxelFilter;
	[SerializeField, HideInInspector] MeshFilter meshFilter;
	[SerializeField] internal VoxelPalette voxelPalette;

	[SerializeField] internal BlockGenerationSetting blockSetting;

	[SerializeField] Mesh destinationMesh;

	[SerializeField] DisplayMember regenerateMesh = new(nameof(RegenerateMeshes));
	[SerializeField] DisplayMember createMeshFile = new(nameof(CreateMeshFile));

	[SerializeField] bool doBenchmark;
	[SerializeField] TMP_Text debugText;

	public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix;

	public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

	void CreateMeshFile()
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
		if (doBenchmark)
			_benchmarkTimer ??= new BenchmarkTimer();
		else
			_benchmarkTimer = null;

		_benchmarkTimer?.StartModule("Clear Lists");



		_vertices.Clear();
		_normals.Clear();
		_uv.Clear();
		_triangles.Clear();
		_currentTriangleIndex = 0;
		_descriptors.Clear();


		_benchmarkTimer?.StartModule("Generate Blocks based on VoxelMap");

		BlockMapGenerator.blockSetup = blockSetting;
		List<List<Block>> blocks = BlockMapGenerator.CalculateBlocks(Map, voxelPalette.Length);

		int i = 0;
		foreach (VoxelPaletteItem paletteItem in voxelPalette.Items)
		{
			RegenerateMeshData(blocks[i], paletteItem, i);
			i++;
		}

		_benchmarkTimer?.StartModule("Setup Mesh");
		destinationMesh.Clear();
		destinationMesh.indexFormat = _vertices.Count >= vertexLimitOf16Bit ?
			IndexFormat.UInt32 : IndexFormat.UInt16;

		_benchmarkTimer?.StartModule("Copy Vertex data to Mesh");
		destinationMesh.vertices = _vertices.ToArray();
		destinationMesh.normals = _normals.ToArray();
		destinationMesh.uv = _uv.ToArray();

		_benchmarkTimer?.StartModule("Copy Triangle data to Mesh");
		destinationMesh.triangles = _triangles.ToArray();

		_benchmarkTimer?.StartModule("Copy SubMesh data");
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


	void RegenerateMeshData(List<Block> blocks, VoxelPaletteItem paletteItem, int index)
	{
		if (destinationMesh == null)
		{
			destinationMesh = new()
			{
				name = voxelFilter.MapName
			};
		}

		if (doBenchmark)
			_benchmarkTimer.StartModule(("Generate Vertex & Triangle data" + index));
		BuildMeshFromBlocks(paletteItem.blockLibrary, blocks, _vertices, _normals, _uv, _triangles);

		if (doBenchmark)
			_benchmarkTimer.StartModule(("Generate SubMesh data" + index));

		int triangleCount = _triangles.Count - _currentTriangleIndex;
		_descriptors.Add(new SubMeshDescriptor(_currentTriangleIndex, triangleCount));
		_currentTriangleIndex = _triangles.Count;
	}

	static void BuildMeshFromBlocks(VoxelBlockLibrary blockLibrary, List<Block> blocksList,
	List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
	{
		foreach (Block block in blocksList)
		{
			if (!blockLibrary.TryGetMesh(block, out CustomMesh mesh))
				continue;
			Vector3 offset = block.Center;
			vertices.AddRange(mesh.vertices.Select(v => v + offset));
			normals.AddRange(mesh.normals);
			uv.AddRange(mesh.uv);
			triangles.AddRange(mesh.triangles.Select(t => t + vertices.Count - mesh.vertices.Length));
		}
	}

	// Mesh Generation

	
	private void OnDrawGizmos()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.color = new(0, 0, 0, 0.3f);
		foreach (var corners in BlockMapGenerator._allNegativeCornersByMaterial)
			foreach (var corner in corners)
			{
				float r = Random.Range(0.15f, 0.25f);
				Vector3 position = corner.Item1 + Vector3.one * 0.5f + (Vector3)corner.Item2 * 0.25f;
				Gizmos.DrawWireCube(position, Vector3.one * r);
			}

		Gizmos.color = new(0, 0, 1, 0.3f);
		foreach (var edges in BlockMapGenerator._allNegativeEdgesByMaterial)
			foreach (var edge in edges)
			{
				float r = Random.Range(0.15f, 0.25f);
				Vector3 position = edge.Item1 + Vector3.one * 0.5f + (Vector3)edge.Item2 * 0.25f;
				Gizmos.DrawWireCube(position, Vector3.one * r);
				Vector3 dir = edge.Item3.ToVector() * r;
				Gizmos.DrawLine(position + dir, position - dir);

			}
		Gizmos.matrix = Matrix4x4.identity;
	}
	
}