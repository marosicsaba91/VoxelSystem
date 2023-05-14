using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	internal delegate void VoxelBuilderFunction(
		VoxelMap voxelMap,
		int voxelIndex,
		List<Vector3> vertices,
		List<Vector3> normals,
		List<Vector2> uv,
		List<int> triangles);
}