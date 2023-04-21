using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[ExecuteAlways]
public class VoxelRenderer : MonoBehaviour
{
    [SerializeField] VoxelMapScriptableObject map; 
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

            if (mesh == null)
                mesh = VoxelBuilder.VoxelMapToMesh(map.map, GenerateMesh);

            return mesh;
        }
    }

    public VoxelMap Map => map == null? null : map.map;

    static readonly List<Block> _blockCache = new();
    Dictionary<BlockKey, CustomMesh> _meshCache;

    void OnValidate()
    {
        mesh = null;
    }

    void RebuildMesh()
    {
        if (map == null) return;
        if (blockLibrary == null) return;

        mesh = VoxelBuilder.VoxelMapToMesh(map.map, GenerateMesh);
    }

    void GenerateMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
    {
        if (_blockCache.IsEmpty())
            BlockVoxelBuilder.CalculateBlocks(voxelMap, _blockCache, mergeCloseEdgesOnTestMesh);
         
        BlockVoxelBuilder.BuildMeshFromBlocks(blockLibrary, _blockCache, vertices, normals, uv, triangles);
    }
}