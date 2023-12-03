using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoxelSystem
{

	[CreateAssetMenu(fileName = "CubeVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: Cube", order = 1)]
	public class VoxelShape_Cube : VoxelShapeBuilder
	{
		[Header("Manual Meshes for each sides")]
		[SerializeField] List<Mesh> topSide = new();
		[SerializeField] List<Mesh> bottomSide = new();
		[SerializeField] List<Mesh> rightSide = new();
		[SerializeField] List<Mesh> leftSide = new();
		[SerializeField] List<Mesh> forwardSide = new();
		[SerializeField] List<Mesh> backSide = new();
		[SerializeField] bool transformFromRightHanded = true;

		[Header("Texture")]
		[SerializeField] CubeUVSetup textureUvCoordinates;
		[SerializeField] bool useTextureSettingOnCustomMeshes = true;

		[Header("Other Setup")]
		[SerializeField] bool drawSidesOnTheMapEdge = true;
		[SerializeField] bool drawSidesBetweenDifferentVoxelTypes = false;


		// Generated Data
		[SerializeField, HideInInspector] MeshCache[] sideMeshCache = new MeshCache[6];

		protected override bool IsInitialized => sideMeshCache[0] != null && sideMeshCache[0].Count > 0;

		public sealed override bool IsSideFilled(GeneralDirection3D dir) => true;



		readonly List<CubeSide> allSides = new();
		static readonly int[] positiveWinding = { 0, 1, 2, 0, 2, 3 };
		static readonly int[] negativeWinding = { 0, 2, 1, 0, 3, 2 };
		static readonly GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;
		protected override void InitializeMeshCache()
		{
			if (sideMeshCache == null || sideMeshCache.Length != 6)
				sideMeshCache = new MeshCache[6];

			for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
			{
				MeshCache meshCache = sideMeshCache[dirIndex];
				if (meshCache == null)
					meshCache = sideMeshCache[dirIndex] = new MeshCache();
				else
					meshCache.Clear();

				GeneralDirection3D direction = directions[dirIndex];

				List<Mesh> setupMeshes = GetSetupMeshes(direction);
				if (setupMeshes.Count == 0)
					meshCache.Add(GenerateDefaultSide(direction, textureUvCoordinates));
				else
				{
					Mesh mesh = setupMeshes[0];
					ArrayMesh arrayMesh = ArrayMesh.CreateFromMesh(mesh, transformFromRightHanded);

					if (useTextureSettingOnCustomMeshes)
					{
						Axis3D axis = direction.GetAxis();
						arrayMesh.ProjectUV(textureUvCoordinates.GetRect(direction), axis);
					}
					
					meshCache.Add(arrayMesh);
					//meshCache.AddRange(setupMeshes, transformFromRightHanded);
				}
			}
		}

		static readonly Vector3 half = Vector3.one * 0.5f;
		public static ArrayMesh GenerateDefaultSide(GeneralDirection3D direction, CubeUVSetup cubeTextureCoordinates)
		{
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

			return new()
			{
				vertices = vs,
				normals = ns,
				uv = uvs,
				triangles = direction.IsPositive() ? positiveWinding : negativeWinding
			};
		}

		List<Mesh> GetSetupMeshes(GeneralDirection3D direction) => direction switch
		{
			GeneralDirection3D.Right => rightSide,
			GeneralDirection3D.Left => leftSide,
			GeneralDirection3D.Up => topSide,
			GeneralDirection3D.Down => bottomSide,
			GeneralDirection3D.Forward => forwardSide,
			GeneralDirection3D.Back => backSide,
			_ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
		};

		protected sealed override void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder)
		{
			GenerateSideList(map, voxelPositions);
			UpdateMeshData(meshBuilder);
		}

		void GenerateSideList(VoxelMap map, List<Vector3Int> voxelIndices)
		{
			allSides.Clear();
			Vector3Int mapSize = map.FullSize;
			for (int i = 0; i < voxelIndices.Count; i++)
			{
				Vector3Int voxelIndex = voxelIndices[i];
				int voxel = map.GetVoxel(voxelIndex);

				if (voxel.IsEmpty()) continue;

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
							if (drawSidesBetweenDifferentVoxelTypes)
							{
								if (neighbour == voxel) continue;
							}
							else if (neighbour.IsFilled()) continue;
						}
						else if (!drawSidesOnTheMapEdge && map.FullSize != Vector3Int.one) continue; 

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

		void UpdateMeshData(MeshBuilder meshBuilder)
		{

			for (int sideIndex = 0; sideIndex < allSides.Count; sideIndex++)
			{
				CubeSide side = allSides[sideIndex];
				ArrayMesh sideMesh = sideMeshCache[(int)side.direction].GetRandom(Random.Range(0, 100));  // TODO: use seed 

				Vector3 center = side.voxelIndex + half;
				meshBuilder.Add(sideMesh, center);
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