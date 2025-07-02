using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem.MeshUtility
{
	[Serializable]
	public class MeshBuilderList
	{
		[SerializeField] List<MeshBuilder> meshes = new();

		public void Add(MeshBuilder mesh) => meshes.Add(mesh);
		public void Clear() => meshes.Clear();
		public MeshBuilder Get(int index) => meshes[index];
		public int Count => meshes.Count;
		public MeshBuilder GetRandom(int seed) => meshes[seed % meshes.Count];

		public void Add(Mesh mesh, bool fromRightHanded = false) =>
			Add(new(mesh, fromRightHanded));

		public void AddRange(List<Mesh> meshes, bool fromRightHanded = false)
		{
			for (int i = 0; i < meshes.Count; i++)
				Add(new(meshes[i], fromRightHanded));
		}
	}
}