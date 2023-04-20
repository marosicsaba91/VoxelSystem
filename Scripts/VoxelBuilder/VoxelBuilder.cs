using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
    public abstract class VoxelBuilder : ScriptableObject
    {
        static readonly List<Vector3> _vertices = new();
        static readonly List<Vector3> _normals = new();
        static readonly List<Vector2> _uv = new();
        static readonly List<int> _triangles = new();

        internal delegate void BuilderFunction(
            VoxelMap voxelMap,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uv,
            List<int> triangles);

        public Mesh VoxelMapToMesh(VoxelMap map) => VoxelMapToMesh(map, BuildMesh);
        
        internal static Mesh VoxelMapToMesh(VoxelMap map, BuilderFunction builderFunction)
        {
            _vertices.Clear();
            _normals.Clear();
            _uv.Clear();
            _triangles.Clear();
            
            builderFunction(map, _vertices, _normals, _uv, _triangles);

            Mesh mesh = new()
            {
                vertices = _vertices.ToArray(),
                normals = _normals.ToArray(),
                uv = _uv.ToArray(),
                triangles = _triangles.ToArray()
            };

            return mesh;
        }
        
        protected abstract void BuildMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles);
        
        public abstract IEnumerable<PaletteItem> GetPaletteItems();
        
        public abstract int PaletteLength { get; }

        public virtual void DrawGizmos(VoxelMap map) { }
    }
}