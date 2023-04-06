using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
    public abstract class VoxelBuilder : ScriptableObject
    {
        public Mesh VoxelModelToMesh(VoxelMap map)
        {
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uv = new();
            List<int> triangles = new();
            
            BuildMesh(map, vertices, normals, uv, triangles);

            Mesh mesh = new()
            {
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                uv = uv.ToArray(),
                triangles = triangles.ToArray()
            };

            return mesh;
        }
        

        protected abstract void BuildMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles);
        public abstract IEnumerable<PaletteItem> GetPaletteItems();
        
        public abstract int PaletteLength { get; }

        public virtual void DrawGizmos(VoxelMap map)
        {
            
        }
    }
}