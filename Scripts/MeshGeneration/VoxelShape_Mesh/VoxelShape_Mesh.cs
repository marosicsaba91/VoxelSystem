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

	[SerializeField] MeshBuilder[] transformedMeshes = new MeshBuilder[128];
	 

	protected override void InitializeMeshCache()
	{
		for (int i = 0; i < CubicTransformation.allCount; i++)
		{
			CubicTransformation cubicTransformation = new(i);
			Matrix4x4 transformation = cubicTransformation.GetTransformation(autoConvertFromRightHanded);
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

			ushort extraVoxelData = vertexValue.extraVoxelData;
			if (extraVoxelData >= CubicTransformation.allCount)
			{
				extraVoxelData = 0;
				vertexValue.extraVoxelData = 0;
				map.SetVoxel(position, vertexValue);
			}
			MeshBuilder transformedMesh = transformedMeshes[extraVoxelData];

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
			for (int d = 0; d < 6; d++)
			{
				GeneralDirection3D localDirection = (GeneralDirection3D)d;
				CubicTransformation transformation = new(voxel.extraVoxelData);
				GeneralDirection3D globalDir = transformation.TransformDirection(localDirection);
				bool closed = closedSides[localDirection];
				voxel.SetSideClosed(globalDir, closed);
			}

			map.SetVoxel(voxelPosition, voxel);
		}
	}

	List<ExtraVoxelControl> controls;

	public override IReadOnlyList<ExtraVoxelControl> GetExtraControls()
	{
		controls ??= new List<ExtraVoxelControl>()
		{
			new ExtraVoxelControl<GeneralDirection3D> ()
			{
				name = "UpDirection",
				getValue = GetUpDirection,
				setValue = SetUpDirection
			},
			new ExtraVoxelControl<int>()
			{
				name = "Vertical Rotation",
				getValue = GetRotation,
				setValue = SetRotation
			},
			new ExtraVoxelControl<bool>()
			{
				name = "Vertical Flip",
				getValue = GetFlip,
				setValue = SetFlip
			},
		};
		return controls;
	}
	static GeneralDirection3D GetUpDirection(ushort extraVoxelData)
	{
		CubicTransformation cubicTransformation = new(extraVoxelData);
		return cubicTransformation.UpDirection;
	}

	static ushort SetUpDirection(ushort originalExtraValue, GeneralDirection3D value)
	{
		CubicTransformation cubicTransformation = new(originalExtraValue)
		{
			UpDirection = value
		};
		return (ushort)cubicTransformation.GetIndex();
	}
	static int GetRotation(ushort extraVoxelData)
	{
		CubicTransformation cubicTransformation = new(extraVoxelData);
		return cubicTransformation.verticalRotation % 4;
	}
	static ushort SetRotation(ushort originalExtraValue, int value)
	{
		value %= 4;
		CubicTransformation cubicTransformation = new(originalExtraValue)
		{
			verticalRotation = value
		};
		return (ushort)cubicTransformation.GetIndex();
	}


	static bool GetFlip(ushort extraVoxelData)
	{
		CubicTransformation cubicTransformation = new(extraVoxelData);
		return cubicTransformation.verticalFlip;
	}
	static ushort SetFlip(ushort originalExtraValue, bool value)
	{
		CubicTransformation cubicTransformation = new(originalExtraValue)
		{
			verticalFlip = value
		};
		return (ushort)cubicTransformation.GetIndex();
	}

	CubicTransformation GetTransformation(ushort extraVoxelData) => new(extraVoxelData);


	protected override PhysicalVoxelShape PhysicalShape(ushort extraVoxelData)
	{
		CubicTransformation transformation = GetTransformation(extraVoxelData);
		PhysicalVoxelShape physicalVoxelShape = new()
		{
			shapeType = ShapeType.FullBlock,

			solidRight = closedSides[transformation.InverseTransformDirection(GeneralDirection3D.Right)],
			solidLeft = closedSides[transformation.InverseTransformDirection(GeneralDirection3D.Left)],
			solidTop = closedSides[transformation.InverseTransformDirection(GeneralDirection3D.Up)],
			solidBottom = closedSides[transformation.InverseTransformDirection(GeneralDirection3D.Down)],
			solidForward = closedSides[transformation.InverseTransformDirection(GeneralDirection3D.Forward)],
			solidBack = closedSides[transformation.InverseTransformDirection(GeneralDirection3D.Back)],
			 
			levelCount = 0,
			currentLevel = 0,
			levelLeight = 0,
			stairSideUp = GeneralDirection3D.Up,
			stairSide1 = GeneralDirection3D.Right,
			stairSide2 = GeneralDirection3D.Left,
		};
		return physicalVoxelShape;
	}

	public sealed override void AddMeshSides(FlexibleMesh mesh, Vector3Int position, ushort extraData) => mesh.AddCube(position);
}
