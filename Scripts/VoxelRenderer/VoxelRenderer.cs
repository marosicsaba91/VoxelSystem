using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[ExecuteAlways]
[RequireComponent(typeof(VoxelFilter))]
public class VoxelRenderer : MonoBehaviour
{
	[SerializeField, HideInInspector] VoxelFilter voxelFilter;	
	[SerializeField] internal VoxelPalette voxelPalette;
	[SerializeField] internal bool mergeCloseEdgesOnTestMesh;
	[SerializeField] bool doBenchmark;

	[SerializeField] List<SerializableMesh> serializableMeshes = new();
	
	[SerializeField] DisplayMember regenerateMesh = new(nameof(RegenerateMeshes));

	public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix;
	 
	public VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

	void OnValidate()
	{
		voxelFilter = GetComponent<VoxelFilter>();
	}

	void LateUpdate()
	{
		RenderMesh();
	}

	BenchmarkTimer _timer = new BenchmarkTimer();
	internal VoxelPalette VoxelPalette => voxelPalette;

	void RenderMesh()
	{
		if (voxelPalette == null)
			return;


		if(voxelPalette.Length != serializableMeshes.Count)
			RegenerateMeshes();


		int i = 0;
		foreach(VoxelPaletteItem paletteItem in voxelPalette.Items)
		{
			SerializableMesh sMesh = serializableMeshes[i];

			StartBenchmarkModul("Create Mesh from SerializableMesh" , i);
			Mesh mesh = sMesh.GetMesh();

			StartBenchmarkModul("Render Mesh" , i);
			Graphics.DrawMesh(mesh, transform.localToWorldMatrix, paletteItem.material, gameObject.layer);
			i++;
		}

		if (doBenchmark)
		{
			if(_timer.GetTotalTotalMilliseconds() > 0)
				Debug.Log(_timer.ToString());
			_timer.Clear();
		}
	}

	internal void RegenerateMeshes()
	{
		int paletteLength = voxelPalette.Length;
		while (serializableMeshes.Count < paletteLength)
			serializableMeshes.Add(new SerializableMesh());
		while (serializableMeshes.Count > paletteLength)
			serializableMeshes.RemoveAt(serializableMeshes.Count - 1);

		VoxelMap map = Map;
		if (map == null) return;
		 
		int i = 0;
		foreach (VoxelPaletteItem paletteItem in voxelPalette.Items)
		{
			SerializableMesh sMesh = serializableMeshes[i];
			sMesh.GenerateMesh(map, i, GenerateMesh);
			i++;
		}

		if (doBenchmark)
			_timer.Stop();
	}


	// Mesh Generation
	static readonly List<Block> _blockCache = new();

	void GenerateMesh(VoxelMap voxelMap, int i, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
	{
		StartBenchmarkModul("Generate Blocks basedOnMaps", i);
		if (voxelMap == null) return;
		BlockVoxelBuilder.CalculateBlocks(voxelMap, i,  _blockCache, mergeCloseEdgesOnTestMesh);
		VoxelPaletteItem item = voxelPalette.GetItem(i);
		StartBenchmarkModul("Build SerializableMesh from Blocks", i);
		BlockVoxelBuilder.BuildMeshFromBlocks(item.blockLibrary, _blockCache, vertices, normals, uv, triangles);
	}

	void StartBenchmarkModul(string s, int i)
	{
		if (doBenchmark)
			_timer.StartModule(s + " - " + i);
	}

}