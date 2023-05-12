//using MUtility;
//using System;
//using System.Collections.Generic;
//using UnityEngine;

//namespace VoxelSystem
//{

//	[Serializable]
//	class SerializableMesh
//	{
//		public List<Vector3> vertices = new();
//		public List<Vector3> normals = new();
//		public List<Vector2> uv = new();
//		public List<int> triangles = new();

//		const int vertexLimitOf16Bit = 65536;
//		[SerializeField] bool meshIsReady = true;

//		Mesh _cachedMesh;

//		internal void GenerateMesh(VoxelMap map, int voxelIndex, VoxelBuilderFunction builderFunction)
//		{
//			vertices.Clear();
//			normals.Clear();
//			uv.Clear();
//			triangles.Clear();

//			builderFunction(map, voxelIndex, vertices, normals, uv, triangles);
//			meshIsReady = false;
//		}

//		internal Mesh GetMesh()
//		{
//			if (_cachedMesh != null && meshIsReady)
//				return _cachedMesh;

//			UnityEngine.Rendering.IndexFormat indexFormat = vertices.Count >= vertexLimitOf16Bit ?
//				UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

//			_cachedMesh = new()
//			{
//				indexFormat = indexFormat,
//				vertices = vertices.ToArray(),
//				normals = normals.ToArray(),
//				uv = uv.ToArray(),
//				triangles = triangles.ToArray()
//			};

//			meshIsReady = true;

//			return _cachedMesh;
//		}
//	}
//}
