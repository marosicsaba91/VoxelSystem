using System;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public struct CustomMesh
	{
		public Vector3[] vertices;
		public Vector3[] normals;
		public Vector2[] uv;
		public int[] triangles;

		public void RecalculateWindings()
		{
			for (int i = 0; i < triangles.Length; i += 3)
			{
				int vi = triangles[i];
				int vj = triangles[i + 1];
				int vk = triangles[i + 2];

				Vector3 normal0 = normals[vi];
				Vector3 normal1 = normals[vj];
				Vector3 normal2 = normals[vk];

				Vector3 point0 = vertices[vi];
				Vector3 point1 = vertices[vj];
				Vector3 point2 = vertices[vk];

				Vector3 pointNormal = Vector3.Cross(point1 - point0, point2 - point0);
				Vector3 meanNormal = normal0 + normal1 + normal2;

				float angle = Vector3.Angle(pointNormal, meanNormal);
				if (angle > 90f)
				{
					triangles[i] = vj;
					triangles[i + 1] = vi;
				}
			}
		}

		public static CustomMesh CreateFromMesh(Mesh mesh, Matrix4x4 transformation)
		{
			// Get the mesh vertices
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;

			// Create a new array to hold the rotated vertices
			var transformedV = new Vector3[vertices.Length];
			var transformedN = new Vector3[vertices.Length];
			// Apply the rotation to each vertex
			for (int i = 0; i < vertices.Length; i++)
			{
				transformedV[i] = transformation.MultiplyVector(vertices[i]);
				transformedN[i] = transformation.MultiplyVector(normals[i]);

			}
			var customMesh = new CustomMesh
			{
				vertices = transformedV,
				triangles = mesh.triangles,
				uv = mesh.uv,
				normals = transformedN
			};

			customMesh.RecalculateWindings();

			return customMesh;
		}
	}
}