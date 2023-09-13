using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "Cube Voxel", menuName = "Voxel System/Cube Voxel")]
	public class CubeVoxel : UniversalVoxelBuilder
	{
		[SerializeField] bool drawOnMapEdge = true;
		[SerializeField] bool drawBetweenVoxelChange = false;
		[SerializeField] CubeTextureCoordinates cubeTextureCoordinates;

		private void OnValidate() => cubeTextureCoordinates.OnValidate();

		readonly List<CubeSide> allSides = new();
		readonly Dictionary<GeneralDirection3D, CustomMesh> sideMeshCache = new();

		static readonly int[] positiveWinding = { 0, 1, 2, 0, 2, 3 };
		static readonly int[] negativeWinding = { 0, 2, 1, 0, 3, 2 };
		static readonly GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;

		protected override void BeforeMeshGeneration(VoxelMap map, UniversalVoxelPalette palette, int voxelTypeIndex) =>
			GenerateMeshesCache();

		protected override void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int voxelTypeIndex,
			List<Vector3> vertices,
			List<Vector3> normals,
			List<Vector2> uv,
			List<int> triangles)
		{
			GenerateSideList(map, voxelPositions, voxelTypeIndex);
			UpdateMeshData(vertices, normals, uv, triangles);
		}

		void GenerateMeshesCache()
		{
			GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;
			for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
			{
				GeneralDirection3D direction = directions[dirIndex];
				Vector3Int normal = direction.ToVectorInt();

				GeneralDirection3D d1 = direction.Previous();
				GeneralDirection3D d2 = direction.Next();

				if (!d1.IsPositive())
					d1 = d1.Opposite();
				if (!d2.IsPositive())
					d2 = d2.Opposite();

				Vector3 n = (Vector3)normal;
				Vector3 p1 = d1.ToVector() * 0.5f;
				Vector3 p2 = d2.ToVector() * 0.5f;
				Vector3 nh = n * 0.5f;

				Rect rect = cubeTextureCoordinates.GetRect(direction);

				CustomMesh customMesh = new()
				{
					vertices = new[] { nh - p1 - p2, nh - p1 + p2, nh + p1 + p2, nh + p1 - p2 },
					normals = new[] { n, n, n, n },
					uv = new[] { rect.BottomLeft(), rect.TopLeft(), rect.TopRight(), rect.BottomRight() },
					triangles = direction.IsPositive() ? positiveWinding : negativeWinding
				};

				sideMeshCache.AddOrChangeValue(direction, customMesh);
			}
		}
		void GenerateSideList(VoxelMap map, List<Vector3Int> voxelIndices, int voxelTypeIndex)
		{
			allSides.Clear();
			Vector3Int mapSize = map.FullSize;
			for (int i = 0; i < voxelIndices.Count; i++)
			{
				Vector3Int voxelIndex = voxelIndices[i];
				int voxel = map.GetVoxel(voxelIndex);

				if (voxel.IsEmpty()) continue;
				if (voxel.GetVoxelTypeIndex() != voxelTypeIndex) continue;

				int materialIndex = voxel.GetMaterialIndex();

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
							if (drawBetweenVoxelChange)
							{
								if (neighbour == voxel) continue;
							}
							else if (neighbour.IsFilled()) continue;
						}
						else if (!drawOnMapEdge) continue;


						allSides.Add(new CubeSide
						{
							direction = direction,
							voxelIndex = voxelIndex,
							materialIndex = materialIndex
						});
					}
				}
			}
		}
		void UpdateMeshData(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
		{
			int vertexIndex = vertices.Count;
			Vector3 half = Vector3.one * 0.5f;

			for (int sideIndex = 0; sideIndex < allSides.Count; sideIndex++)
			{
				CubeSide side = allSides[sideIndex];
				CustomMesh sideMesh = sideMeshCache[side.direction];
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

	}

	struct CubeSide
	{
		public GeneralDirection3D direction;
		public Vector3Int voxelIndex;
		public int materialIndex;
		public Rect textureCoordinates;
	}
}