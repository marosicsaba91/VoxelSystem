using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	class MeshCache
	{
		[SerializeField] List<ArrayMesh> meshes = new();

		public void Add(ArrayMesh mesh) => meshes.Add(mesh);
		public void Clear() => meshes.Clear();
		public ArrayMesh Get(int index) => meshes[index];
		public int Count => meshes.Count;
		public ArrayMesh GetRandom(int seed) => meshes[seed % meshes.Count];

		public void Add(Mesh mesh, bool fromRightHanded = false) =>
			Add(ArrayMesh.CreateFromMesh(mesh, fromRightHanded));

		public void AddRange(List<Mesh> meshes, bool fromRightHanded = false)
		{
			for (int i = 0; i < meshes.Count; i++)
				Add(ArrayMesh.CreateFromMesh(meshes[i], fromRightHanded));
		}
	}
}