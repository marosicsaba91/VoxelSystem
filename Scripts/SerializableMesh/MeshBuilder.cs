using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelSystem
{
	[Serializable]
	public class MeshBuilder
	{
		public List<Vector3> vertices = new();
		public List<Vector3> normals = new();
		public List<Vector2> uv = new();
		public List<int> triangles = new();
		public List<SubMeshDescriptor> descriptors = new();

		[SerializeField] int _lastMaterialStartTriangleIndex = 0;
		public int VertexCount => vertices.Count;
		public int TriangleCount => triangles.Count;

		static readonly Matrix4x4 rightToLeftHanded = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90, 0, 0), new Vector3(-1, -1, 1));

		public static MeshBuilder CreateFromMesh(Mesh mesh, Matrix4x4 transformation)
		{
			int vCount = mesh.vertexCount;

			MeshBuilder meshBuilder = new();

			meshBuilder.vertices.Capacity = vCount;
			meshBuilder.normals.Capacity = vCount;
			meshBuilder.uv.Capacity = vCount;

			for (int i = 0; i < vCount; i++)
			{
				meshBuilder.vertices.Add(transformation.MultiplyVector(mesh.vertices[i]));
				meshBuilder.normals.Add(transformation.MultiplyVector(mesh.normals[i]));
				meshBuilder.uv.Add(mesh.uv[i]);
			}

			int tCount = mesh.triangles.Length;
			meshBuilder.triangles.Capacity = tCount;
			for (int i = 0; i < tCount; i++)
				meshBuilder.triangles.Add(mesh.triangles[i]);

			meshBuilder.RecalculateWindings();
			return meshBuilder;
		}
		public static MeshBuilder CreateFromMesh(Mesh mesh, bool fromRightHanded = false)
		{
			if (fromRightHanded)
				return CreateFromMesh(mesh, rightToLeftHanded);
			else
			{
				return new MeshBuilder()
				{
					vertices = new(mesh.vertices),
					triangles = new(mesh.triangles),
					uv = new(mesh.uv),
					normals = new(mesh.normals)
				};
			}
		}

		public void RecalculateWindings()
		{
			for (int i = 0; i < triangles.Count; i += 3)
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

		public void Clear()
		{
			vertices.Clear();
			normals.Clear();
			uv.Clear();
			triangles.Clear();
			descriptors.Clear();
			_lastMaterialStartTriangleIndex = 0;
		}

		public Mesh ToMesh()
		{
			Mesh mesh = new()
			{
				vertices = vertices.ToArray(),
				normals = normals.ToArray(),
				uv = uv.ToArray(),
				triangles = triangles.ToArray()
			};

			mesh.RecalculateBounds();
			return mesh;
		}

		public void CopyToMesh(Mesh destinationMesh)
		{
			const int vertexLimitOf16Bit = 65536;
			destinationMesh.Clear();

			destinationMesh.indexFormat = vertices.Count >= vertexLimitOf16Bit ?
				IndexFormat.UInt32 : IndexFormat.UInt16;
			destinationMesh.vertices = vertices.ToArray();
			destinationMesh.normals = normals.ToArray();
			destinationMesh.uv = uv.ToArray();
			destinationMesh.triangles = triangles.ToArray();
		

			if (descriptors.Count > 0)
			{
				destinationMesh.subMeshCount = descriptors.Count;
				for (int j = 0; j < descriptors.Count; j++)
				{
					SubMeshDescriptor descriptor = descriptors[j];
					destinationMesh.SetSubMesh(j, descriptor);
				} 
			}

			destinationMesh.RecalculateBounds();
		}

		internal void ApplyScale(Vector3 scale)
		{
			for (int i = 0; i < vertices.Count; i++)
			{
				vertices[i] = Vector3.Scale(vertices[i], scale);
				normals[i] = Vector3.Scale(normals[i], scale);
			}
			RecalculateWindings();
		}

		internal void ApplyRotation(Quaternion rotation)
		{
			for (int i = 0; i < vertices.Count; i++)
			{
				vertices[i] = rotation * vertices[i];
				normals[i] = rotation * normals[i];
			}
		}

		internal void ClearAndCopyFrom(MeshBuilder mesh)
		{
			Clear();

			for (int i = 0; i < mesh.vertices.Count; i++)
			{
				vertices.Add(mesh.vertices[i]);
				normals.Add(mesh.normals[i]);
				uv.Add(mesh.uv[i]);
			}

			for (int i = 0; i < mesh.triangles.Count; i++)
				triangles.Add(mesh.triangles[i]);
		}

		public void Add(MeshBuilder mesh) 
		{
			int startIndex = vertices.Count;

			for (int i = 0; i < mesh.vertices.Count; i++)
			{
				vertices.Add(mesh.vertices[i]);
				normals.Add(mesh.normals[i]);
				uv.Add(mesh.uv[i]);
			}

			for (int i = 0; i < mesh.triangles.Count; i++)
			{
				triangles.Add(mesh.triangles[i] + startIndex);
			}
		}

		public void Add(ArrayMesh mesh)
		{
			int startIndex = vertices.Count;

			for (int i = 0; i < mesh.vertices.Length; i++)
			{
				vertices.Add(mesh.vertices[i]);
				normals.Add(mesh.normals[i]);
				uv.Add(mesh.uv[i]);
			}

			for (int i = 0; i < mesh.triangles.Length; i++)
			{
				triangles.Add(mesh.triangles[i] + startIndex);
			}
		}

		public void Add(MeshBuilder mesh, Vector3 translate)
		{
			int startIndex = vertices.Count;

			for (int i = 0; i < mesh.vertices.Count; i++)
			{
				vertices.Add(mesh.vertices[i] + translate);
				normals.Add(mesh.normals[i]);
				uv.Add(mesh.uv[i]);
			}

			for (int i = 0; i < mesh.triangles.Count; i++)
			{
				triangles.Add(mesh.triangles[i] + startIndex);
			}
		}

		public void Add(ArrayMesh mesh, Vector3 translate)
		{
			int startIndex = vertices.Count;

			for (int i = 0; i < mesh.vertices.Length; i++)
			{
				vertices.Add(mesh.vertices[i] + translate);
				normals.Add(mesh.normals[i]);
				uv.Add(mesh.uv[i]);
			}

			for (int i = 0; i < mesh.triangles.Length; i++)
			{
				triangles.Add(mesh.triangles[i] + startIndex);
			}
		}

		public void AddVertex(Vector3 vertex, Vector3 normal, Vector2 uv) 
		{
			vertices.Add(vertex);
			normals.Add(normal);
			this.uv.Add(uv);
		}

		internal void AddTriangle(int v1, int v2, int v3) 
		{
			triangles.Add(v1);
			triangles.Add(v2);
			triangles.Add(v3);
		}

		public void EndMaterialDescriptor() 
		{ 
			int triangleCount = TriangleCount - _lastMaterialStartTriangleIndex;
			descriptors.Add(new SubMeshDescriptor(_lastMaterialStartTriangleIndex, triangleCount));
			_lastMaterialStartTriangleIndex = TriangleCount;
		}

		public void Transform(Matrix4x4 transformation)
		{
			for (int i = 0; i < vertices.Count; i++)
			{
				vertices[i] = transformation.MultiplyVector(vertices[i]);
				normals[i] = transformation.MultiplyVector(normals[i]);
			}

			if (transformation.determinant < 0)
				RecalculateWindings();
		}

		internal void ProjectUV(Rect rect, Axis3D getAxis)
		{

			for (int i = 0; i < vertices.Count; i++)
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