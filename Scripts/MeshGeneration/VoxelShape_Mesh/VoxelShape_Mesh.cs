using MeshUtility;
using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[CreateAssetMenu(fileName = "MeshVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: Mesh", order = EditorConstants.soOrder_VoxelShape)]

public class VoxelShape_Mesh : VoxelShapeBuilder
{
	[Space]
	[SerializeField] Mesh mesh;
	[SerializeField] bool autoConvertFromRightHanded = true;
	[SerializeField] SideFlags closedSides = new(false);

	// Cached Data
	[SerializeField, HideInInspector] MeshBuilder[] transformedMeshes = new MeshBuilder[128];

	public sealed override bool SupportsTransformation => true;

	protected override void InitializeCachedData()
	{
		for (byte i = 0; i < CubicTransformation.allTransformationCount; i++)
		{
			CubicTransformation cubicTransformation = new(i);
			Matrix4x4 transformation = cubicTransformation.GetTransformationMatrix(autoConvertFromRightHanded);
			transformedMeshes[i] = new MeshBuilder(mesh, transformation);
		}
	}

	protected override bool IsInitialized => !transformedMeshes.IsNullOrEmpty();

	protected sealed override void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder)
	{
		for (int voxelIndex = 0; voxelIndex < voxelPositions.Count; voxelIndex++)
		{
			Vector3Int position = voxelPositions[voxelIndex];
			Voxel vertexValue = map.GetVoxel(position);

			byte transformation = vertexValue.cubicTransformationIndex;
			if (transformation >= CubicTransformation.allTransformationCount)
			{
				transformation = 0;
				vertexValue.cubicTransformationIndex = 0;
				map.SetVoxel(position, vertexValue);
			}
			MeshBuilder transformedMesh = transformedMeshes[transformation];

			Vector3 center = position + half;
			meshBuilder.Add(transformedMesh, center);
		}
	}

	protected override void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions)
	{
		for (int i = 0; i < voxelPositions.Count; i++)
		{
			Vector3Int voxelPosition = voxelPositions[i];
			Voxel voxel = map.GetVoxel(voxelPosition);
			voxel.closednessInfo = 0;
			CubicTransformation transformation = voxel.CubicTransformation;
			for (int d = 0; d < 6; d++)
			{
				GeneralDirection3D localDirection = (GeneralDirection3D)d;
				GeneralDirection3D globalDir = transformation.TransformDirection(localDirection);
				bool closed = closedSides[localDirection];
				voxel.SetSideClosed(globalDir, closed);
			}

			map.SetVoxel(voxelPosition, voxel);
		}
	}
}

