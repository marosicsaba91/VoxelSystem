using UnityEngine;
namespace VoxelSystem
{
    public static class MeshUtility
    {
        public static Mesh GetTransformedMesh(Mesh mesh, Matrix4x4 transformation)
        {
            // Get the mesh vertices
            var vertices = mesh.vertices;
            var normals = mesh.normals;

            // Create a new array to hold the rotated vertices
            var transformedV = new Vector3[vertices.Length];
            var transformedM = new Vector3[vertices.Length];

            // Apply the rotation to each vertex
            for (var i = 0; i < vertices.Length; i++)
            {
                transformedV[i] = transformation.MultiplyVector(vertices[i]);
                transformedM[i] = transformation.MultiplyVector(normals[i]);
            }
            
            // Debug.Log("rotatedVertices: " + rotatedVertices.Length);

            // Create a new mesh with the rotated vertices
            var rotatedMesh = new Mesh
            {
                vertices = transformedV,
                triangles = mesh.triangles,
                uv = mesh.uv,
                normals = transformedM
            };
            rotatedMesh.RecalculateNormals();
            return rotatedMesh;
        }
    }
}