using MUtility; 
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[CreateAssetMenu(fileName = "MeshVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: Mesh", order = EditorConstants.soOrder_VoxelShape)]

public class VoxelShape_Mesh : VoxelShapeBuilder
{
	[SerializeField] Mesh mesh;
	[SerializeField] bool autoConvertFromRightHanded = true;
	[SerializeField] SideFlags isSideFilled = new(false);

	static readonly Matrix4x4 rightToLeftHanded = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90, 0, 0), new Vector3(-1, -1, 1));
	
	[SerializeField, HideInInspector] ArrayMesh[] transformedMeshes = new ArrayMesh[128];

	protected override void InitializeMeshCache()
	{ 
		Matrix4x4 baseMatrix = autoConvertFromRightHanded ? rightToLeftHanded : Matrix4x4.identity;
		for (int i = 0; i < CubicTransformation.allCount; i++)
		{
			CubicTransformation cubicTransformation = new(i);
			Matrix4x4 transformation = cubicTransformation.GetTransformation() * baseMatrix;
			transformedMeshes[i] = ArrayMesh.CreateFromMesh(mesh, transformation);
		}
	}

	protected override bool IsInitialized => !transformedMeshes.IsNullOrEmpty();

	static readonly Vector3 half = Vector3.one * 0.5f;

	protected sealed override void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder)
	{
		for (int voxelIndex = 0; voxelIndex < voxelPositions.Count; voxelIndex++)
		{
			Vector3Int position = voxelPositions[voxelIndex];
			int vertexValue = map.GetVoxel(position);

			ushort extraVoxelData = vertexValue.GetExtraVoxelData();
			if (extraVoxelData >= CubicTransformation.allCount)
			{
				extraVoxelData = 0;
				map.SetVoxel(position, vertexValue.SetExtraVoxelData(0));
			} 
			ArrayMesh transformedMesh = transformedMeshes[extraVoxelData];

			Vector3 center = position + half;
			meshBuilder.Add(transformedMesh, center);
		}
	}

	protected override void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions, byte[] sideClosedness)
	{
		for (int i = 0; i < voxelPositions.Count; i++)
		{
			int index = ArrayVoxelMap.GetIndex(voxelPositions[i], map.FullSize);
			sideClosedness[index] = 0; // TODO: Implement this properly
		} 
		// Use IsSideFilled(dir, voxelValue);
	}

	public bool IsSideFilled(GeneralDirection3D dir, int voxelValue)
	{ 
		ushort extraVoxelData = voxelValue.GetExtraVoxelData();
		CubicTransformation cubicTransformation = new(extraVoxelData); 
		return isSideFilled[cubicTransformation.TransformDirection(dir)];
	}


	List<ExtraControl> controls;

	public override IReadOnlyList<ExtraControl> GetExtraControls()
	{
		controls ??= new List<ExtraControl>()
		{
			new ExtraControl<GeneralDirection3D> ()
			{
				name = "UpDirection",
				getValue = GetUpDirection,
				setValue = SetUpDirection
			},
			new ExtraControl<int>()
			{
				name = "Vertical Rotation",
				getValue = GetRotation,
				setValue = SetRotation
			},
			new ExtraControl<bool>()
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
}
