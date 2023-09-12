using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[ExecuteAlways]
[RequireComponent(typeof(VoxelObject))]
public class CubeMeshGenerator : VoxelMeshGenerator<CubeVoxelPalette, CubeVoxelPaletteItem>
{


	struct Side
	{
		public GeneralDirection3D direction;
		public Vector3Int voxelIndex;
		public int uvIndex;
	}

	readonly List<List<Side>> sidesByMaterial = new();

	protected sealed override void BeforeMeshGeneration(VoxelMap map, CubeVoxelPalette palette)
	{
		while (sidesByMaterial.Count < palette.Length)
			sidesByMaterial.Add(new List<Side>());

		foreach (List<Side> sides in sidesByMaterial)
			sides.Clear();

		Vector3Int mapSize = map.FullSize;
		int maxMaterial = palette.Length - 1;

		GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;

		for (int x = 0; x < mapSize.x; x++)
			for (int y = 0; y < mapSize.y; y++)
				for (int z = 0; z < mapSize.z; z++)
				{
					Vector3Int voxelIndex = new(x, y, z);
					int voxel = map.GetVoxel(voxelIndex);
					if (voxel.IsEmpty())
						continue;

					for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
					{
						GeneralDirection3D direction = directions[dirIndex];
						{
							Vector3Int normal = direction.ToVectorInt();
							Vector3Int ni = voxelIndex + normal;
							bool voxelExists = ni.x >= 0 && ni.y >= 0 && ni.z >= 0 && ni.x < mapSize.x && ni.y < mapSize.y && ni.z < mapSize.z;
							if (voxelExists)
							{
								int neighbour = map.GetVoxel(ni);
								if (neighbour.IsFilled()) continue;
							}
							int materialIndex = voxel > maxMaterial ? maxMaterial : voxel;
							sidesByMaterial[materialIndex].Add(new Side { direction = direction, voxelIndex = voxelIndex, uvIndex = 0 });
						}
					}
				}
	}

	protected sealed override void GenerateMeshData(int paletteIndex, CubeVoxelPaletteItem paletteItem, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
	{
		paletteItem.FreshMeshes();
		List<Side> sides = sidesByMaterial[paletteIndex];

		int vertexIndex = vertices.Count;
		Vector3 half = Vector3.one * 0.5f;

		for (int si = 0; si < sides.Count; si++)
		{
			Side side = sides[si];
			CustomMesh sideMesh = paletteItem.GetMesh(side.direction);
			Vector3 center = side.voxelIndex + half;

			for (int vi = 0; vi < 4; vi++)
			{
				vertices.Add(sideMesh.vertices[vi] + center);
				normals.Add(sideMesh.normals[vi]);
				uv.Add(sideMesh.uv[vi]);
			}
			for (int ti = 0; ti < 6; ti++)
			{
				triangles.Add(sideMesh.triangles[ti] + vertexIndex);
			}
			vertexIndex += 4;
		}
	}

	internal sealed override VoxelMeshGenerator<CubeVoxelPalette, CubeVoxelPaletteItem> AddACopy(GameObject newGO)
	{
		CubeMeshGenerator newComponent = newGO.AddComponent<CubeMeshGenerator>();
		return newComponent;
	}
}