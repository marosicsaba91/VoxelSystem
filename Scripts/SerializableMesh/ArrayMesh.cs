using MUtility;
using System;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace VoxelSystem
{
	[Serializable]
	public struct ArrayMesh
	{
		public Vector3[] vertices;
		public Vector3[] normals;
		public Vector2[] uv;
		public int[] triangles;


		static readonly Matrix4x4 rightToLeftHanded = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90, 0, 0), new Vector3(-1, -1, 1));

		public bool IsEmpty => vertices.IsNullOrEmpty();

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

		public static ArrayMesh CreateFromMesh(Mesh mesh, bool fromRightHanded = false)
		{
			if (fromRightHanded)
				return CreateFromMesh(mesh, rightToLeftHanded);
			else
			{
				return new ArrayMesh()
				{
					vertices = mesh.vertices,
					triangles = mesh.triangles,
					uv = mesh.uv,
					normals = mesh.normals
				};
			}
		}

		public static ArrayMesh CreateFromMesh(Mesh mesh, Matrix4x4 transformation)
		{
			// Get the previewMesh vertices
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;

			// Create a new array to hold the rotated vertices
			Vector3[] transformedV = new Vector3[vertices.Length];
			Vector3[] transformedN = new Vector3[vertices.Length];
			// Apply the rotation to each vertex
			for (int i = 0; i < vertices.Length; i++)
			{
				transformedV[i] = transformation.MultiplyVector(vertices[i]);
				transformedN[i] = transformation.MultiplyVector(normals[i]);
			}
			ArrayMesh customMesh = new()
			{
				vertices = transformedV,
				triangles = mesh.triangles,
				uv = mesh.uv,
				normals = transformedN
			};

			if (transformation.determinant < 0)
				customMesh.RecalculateWindings();

			return customMesh;
		}

		public void Transform(Matrix4x4 transformation)
		{
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i] = transformation.MultiplyVector(vertices[i]);
				normals[i] = transformation.MultiplyVector(normals[i]);
			}

			if (transformation.determinant < 0)
				RecalculateWindings();
		}

		internal void ProjectUV(Rect rect, Axis3D getAxis)
		{

			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 vx = vertices[i];
				float ui = getAxis switch
				{
					Axis3D.X => vx.y,
					Axis3D.Y => vx.x,
					Axis3D.Z => vx.x,
					_ => 0
				};
				float vi = getAxis switch
				{
					Axis3D.X => vx.z,
					Axis3D.Y => vx.z,
					Axis3D.Z => vx.y,
					_ => 0
				};
				ui += 0.5f;
				vi += 0.5f;


				Vector2 min = rect.min;
				Vector2 max = rect.max;

				float u = Mathf.Lerp(min.x, max.x, ui);
				float v = Mathf.Lerp(min.y, max.y, vi);

				uv[i] = new Vector2(u, v);
			}
		}
	}
}