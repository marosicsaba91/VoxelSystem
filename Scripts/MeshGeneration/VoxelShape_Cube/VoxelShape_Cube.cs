using Random = UnityEngine.Random;
using System.Collections.Generic;
using VoxelSystem.MeshUtility;
using UnityEngine;
using MUtility;
using System;

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
		[SerializeField] bool autoConvertFromRightHanded = true;

		[Header("Texture")]
		[SerializeField] CubeUVSetup textureUvCoordinates = new ();
		[SerializeField] bool useTextureSettingOnCustomMeshes = false;

		[Header("Other SetupFromMesh")]
		[SerializeField] bool drawSidesOnTheMapEdge = true;
		[SerializeField] bool drawSidesBetweenDifferentVoxelTypes = false;
		[SerializeField] bool isTransparent = false;

		// Generated Data
		[SerializeField, HideInInspector] MeshBuilderList[] sideMeshCache = new MeshBuilderList[6];

		protected override bool IsInitialized => sideMeshCache[0] != null && sideMeshCache[0].Count > 0;
		public sealed override bool SupportsTransformation => false;

		readonly List<CubeSide> allSides = new();
		static readonly GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;

		protected override void InitializeCachedData()
		{
			if (sideMeshCache == null || sideMeshCache.Length != 6)
				sideMeshCache = new MeshBuilderList[6];

			for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
			{
				MeshBuilderList meshList = sideMeshCache[dirIndex];
				if (meshList == null)
					meshList = sideMeshCache[dirIndex] = new MeshBuilderList();
				else
					meshList.Clear();

				GeneralDirection3D direction = directions[dirIndex];

				List<Mesh> setupMeshes = GetSetupMeshes(direction);
				if (setupMeshes.Count == 0)
					meshList.Add(textureUvCoordinates.GetCubeSide(direction));
				else
				{
					Mesh mesh = setupMeshes[0];
					MeshBuilder meshBuilder = new(mesh, autoConvertFromRightHanded);

					if (useTextureSettingOnCustomMeshes)
						meshBuilder.ProjectUV(textureUvCoordinates, direction);

					meshList.Add(meshBuilder);
				}
			}
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

		protected override void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions)
		{
			bool close = !isTransparent;
			for (int i = 0; i < voxelPositions.Count; i++)
			{
				Vector3Int voxelPosition = voxelPositions[i];
				Voxel v = map.GetVoxel(voxelPosition);
				v.SetAllSideClose(close);
				map.SetVoxel(voxelPosition, v);
			}
		}

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
				Voxel voxel = map.GetVoxel(voxelIndex);

				if (voxel.IsEmpty()) continue;

				int materialIndex = voxel.materialIndex;

				for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
				{
					GeneralDirection3D direction = directions[dirIndex];
					{
						Vector3Int normal = direction.ToVectorInt();
						Vector3Int ni = voxelIndex + normal;
						bool voxelExists = ni.x >= 0 && ni.y >= 0 && ni.z >= 0 && ni.x < mapSize.x && ni.y < mapSize.y && ni.z < mapSize.z;

						if (voxelExists)
						{
							Voxel neighbor = map.GetVoxel(ni);

							if (neighbor.IsFilled())
							{
								if (neighbor.shapeId == voxel.shapeId &&
									neighbor.materialIndex == voxel.materialIndex)
								{
									continue;
								}

								if (drawSidesBetweenDifferentVoxelTypes) continue;

								if (neighbor.IsSideClosed(direction.Opposite())) continue;
							}
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
				MeshBuilder sideMesh = sideMeshCache[(int)side.direction].GetRandom(Random.Range(0, 100));  // TODO: use seed 

				Vector3 center = side.voxelIndex + half;
				meshBuilder.Add(sideMesh, center);
			}
		}
	}

	public struct CubeSide
	{
		public GeneralDirection3D direction;
		public Vector3Int voxelIndex;
		public int materialIndex;
		public Rect textureCoordinates;
	}
}