using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[ExecuteAlways]
[RequireComponent(typeof(VoxelFilter))]
public class VoxelRenderer : MonoBehaviour
{
	[SerializeField, HideInInspector] VoxelFilter voxelFilter;
	[SerializeField] BlockLibrary blockLibrary;
	[SerializeField] Material material;

	[SerializeField] Mesh mesh;
	[SerializeField] bool mergeCloseEdgesOnTestMesh;

	public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix;
	public Material Material => material;

	public Mesh Mesh
	{
		get
		{
			RebuildMesh();
			return mesh;
		}
	}

	public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetMap();

	static readonly List<Block> _blockCache = new();

	void OnValidate()
	{
		voxelFilter = GetComponent<VoxelFilter>();
		mesh = null;
	}

	void Update()
	{
		VoxelMap map = Map; 
		if (map == null) return;
		if (blockLibrary == null) return;
		mesh = VoxelBuilder.VoxelMapToMesh(map, GenerateMesh);
		Graphics.DrawMesh(mesh, transform.localToWorldMatrix, material, gameObject.layer);
	}


	public void RebuildMesh()
	{
		VoxelMap map = Map;
		// ?????

		if (map == null) return;
		if (blockLibrary == null) return;

		Debug.Log("Regenerate: " + map.Size);
	
		mesh = VoxelBuilder.VoxelMapToMesh(map, GenerateMesh);
		Debug.Log("Regenerated: " + map.Size);
	}

	void GenerateMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
	{
		if (_blockCache.IsEmpty())
			BlockVoxelBuilder.CalculateBlocks(voxelMap, _blockCache, mergeCloseEdgesOnTestMesh);

		BlockVoxelBuilder.BuildMeshFromBlocks(blockLibrary, _blockCache, vertices, normals, uv, triangles);
	}
}