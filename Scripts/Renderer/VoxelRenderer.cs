using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[ExecuteAlways]
[RequireComponent(typeof(VoxelFilter))]
public class VoxelRenderer : MonoBehaviour, IVoxelBuilder
{
	[SerializeField, HideInInspector] VoxelFilter voxelFilter;
	[SerializeField] BlockLibrary blockLibrary;
	[SerializeField] Material material;
	[SerializeField] bool mergeCloseEdgesOnTestMesh;

	[SerializeField] Mesh mesh;

	public float cursorScale;

	[SerializeField] DisplayMember regenerateMesh = new (nameof(RegenerateMesh));

	public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix;
	public Material Material => material;

	public Mesh Mesh
	{
		get
		{
			if(mesh == null)
				RegenerateMesh();
			return mesh;
		}
	}

	public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

	void OnValidate()
	{
		voxelFilter = GetComponent<VoxelFilter>();
		mesh = null;
	}

	void LateUpdate()
	{
		RenderMesh(); 
	}

	void RenderMesh()
	{
		mesh = Mesh; 
		if (mesh == null) return;
		Graphics.DrawMesh(mesh, transform.localToWorldMatrix, material, gameObject.layer);
	}

	internal void RegenerateMesh()
	{
		VoxelMap map = Map; 
		if (map == null) return;
		if (blockLibrary == null) return;

		mesh = VoxelBuilder.VoxelMapToMesh(map, GenerateMesh); 
	}

	// Mesh Generation

	static readonly List<Block> _blockCache = new();
	void GenerateMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
	{ 
		if (voxelMap == null) return;
		BlockVoxelBuilder.CalculateBlocks(voxelMap, _blockCache, mergeCloseEdgesOnTestMesh);

		BlockVoxelBuilder.BuildMeshFromBlocks(blockLibrary, _blockCache, vertices, normals, uv, triangles);
		Debug.Log("Mesh Regenerated");
	}

	public IEnumerable<PaletteItem> GetPaletteItems() 
	{ yield break; }

	public int PaletteLength => 0;




}