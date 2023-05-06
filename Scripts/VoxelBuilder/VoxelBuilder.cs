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
			ArrayVoxelMap voxelMap,
			List<Vector3> vertices,
			List<Vector3> normals,
			List<Vector2> uv,
			List<int> triangles);

		internal delegate void OctBuilderFunction(
			VoxelMap voxelMap,
			List<Vector3> vertices,
			List<Vector3> normals,
			List<Vector2> uv,
			List<int> triangles);

		public Mesh VoxelMapToMesh(ArrayVoxelMap map) => VoxelMapToMesh(map, BuildMesh);
		public Mesh VoxelMapToMesh(VoxelMap map) => VoxelMapToMesh(map, BuildMesh);

		internal static Mesh VoxelMapToMesh(VoxelMap map, OctBuilderFunction builderFunction)
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
		public virtual void DrawGizmos(ArrayVoxelMap map) { }
	}
}