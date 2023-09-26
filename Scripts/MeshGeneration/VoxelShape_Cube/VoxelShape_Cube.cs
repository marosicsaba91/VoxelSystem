using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "CubeVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: Cube", order = 1)]
	public class VoxelShape_Cube : VoxelShapeBuilder
	{
		[SerializeField] bool drawOnMapEdge = true;
		[SerializeField] bool drawBetweenVoxelChange = false;
		[SerializeField] CubeUVSetup cubeTextureCoordinates;
		[SerializeField] ComputeShader computeShader;

		[SerializeField, HideInInspector] ArrayMesh[] sideMeshCache = new ArrayMesh[10];

		readonly List<CubeSide> allSides = new();

		static readonly int[] positiveWinding = { 0, 1, 2, 0, 2, 3 };
		static readonly int[] negativeWinding = { 0, 2, 1, 0, 3, 2 };
		static readonly GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;

		protected sealed override bool IsSideFilled(GeneralDirection3D dir) => true;

		protected override void ValidateInternal()
		{
			if (sideMeshCache == null || sideMeshCache.Length < 10)
				sideMeshCache = new ArrayMesh[10];

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

				Vector3[] vs = { nh - p1 - p2, nh - p1 + p2, nh + p1 + p2, nh + p1 - p2 };
				Vector3[] ns = { n, n, n, n };
				Vector2[] uvs = { rect.BottomLeft(), rect.TopLeft(), rect.TopRight(), rect.BottomRight() };

				ArrayMesh customMesh = new()
				{
					vertices = vs,
					normals = ns,
					uv = uvs,
					triangles = direction.IsPositive() ? positiveWinding : negativeWinding
				};

				sideMeshCache[(int)direction] = customMesh;
			}
		}

		protected sealed override void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder)
		{
			// Vector3Int mapSize = map.FullSize;
			// computeShader.Dispatch(0, mapSize.x / 10, mapSize.y / 10, mapSize.z / 10);


			GenerateSideList(map, voxelPositions, shapeIndex);
			UpdateMeshData(meshBuilder);
		}

		void GenerateSideList(VoxelMap map, List<Vector3Int> voxelIndices, int shapeIndex)
		{
			allSides.Clear();
			Vector3Int mapSize = map.FullSize;
			for (int i = 0; i < voxelIndices.Count; i++)
			{
				Vector3Int voxelIndex = voxelIndices[i];
				int voxel = map.GetVoxel(voxelIndex);

				if (voxel.IsEmpty()) continue;
				if (voxel.GetShapeIndex() != shapeIndex) continue;

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

		void UpdateMeshData( MeshBuilder meshBuilder)
		{
			int vertexIndex = meshBuilder.VertexCount;
			Vector3 half = Vector3.one * 0.5f;

			for (int sideIndex = 0; sideIndex < allSides.Count; sideIndex++)
			{
				CubeSide side = allSides[sideIndex];
				ArrayMesh sideMesh = sideMeshCache[(int)side.direction];
				Vector3 center = side.voxelIndex + half;

				for (int vi = 0; vi < 4; vi++)
				{
					meshBuilder.AddVertex(
						sideMesh.vertices[vi] + center,
						sideMesh.normals[vi],
						sideMesh.uv[vi]);
				}
				for (int ti = 0; ti < 6; ti++)
					meshBuilder.triangles.Add(sideMesh.triangles[ti] + vertexIndex);

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