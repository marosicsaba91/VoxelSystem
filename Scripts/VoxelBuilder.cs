using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
    public abstract class VoxelBuilder : ScriptableObject
    {
        public Mesh VoxelModelToMesh(VoxelMap map)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tringles = new List<int>();

            // DateTime a = DateTime.Now;
            BuildMesh(map, vertices, normals, uv, tringles);

            // DateTime b = DateTime.Now;
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = tringles.ToArray();
            // DateTime c = DateTime.Now;

            // Debug.Log((b - a).TotalMilliseconds + "   " + (c - b).TotalMilliseconds);

            return mesh;
        }
        
        public VoxelPalette palette;

        protected abstract void BuildMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> tringles);
    }
}