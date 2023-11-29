using EasyInspector;
using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[CreateAssetMenu(fileName = "StairVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: Stair", order = EditorConstants.soOrder_VoxelShape)]
public class VoxelShape_Stair : VoxelShapeBuilder
{
	[SerializeField, Range(1, 4)] int slope = 1;

	[Header("Meshes")]
	[SerializeField] EasyMessage meshMessage1 = new("The stair goes up looking FORWARD\n");
	[SerializeField] Mesh stairs;
	[SerializeField] EasyMessage meshMessage2 = new("The corners of the stair look RIGHT-FORWARD\n");
	[SerializeField] Mesh innerCorner;
	[SerializeField] Mesh outerCorner;

	[Header("Sides")]
	[SerializeField] EasyMessage meshMessage3 = new("The side of the stair that goes up looking FORWARD\n");
	[SerializeField] Mesh rightSide;
	[SerializeField] Mesh leftSide;
	[SerializeField] Mesh backSide;
	[SerializeField] Mesh bottomSide;

	[SerializeField] bool autoConvertFromRightHanded = true;

	static readonly Matrix4x4 rightToLeftHanded =
		Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90, 0, 0), new(-1, -1, 1));

	readonly ArrayMesh[] transformedStairs = new ArrayMesh[4];
	readonly ArrayMesh[] transformedInnerCorners = new ArrayMesh[4];
	readonly ArrayMesh[] transformedOuterCorner = new ArrayMesh[4];
	readonly ArrayMesh[] transformedRightSide = new ArrayMesh[4];
	readonly ArrayMesh[] transformedLeftSide = new ArrayMesh[4];
	readonly ArrayMesh[] transformedBackSide = new ArrayMesh[4];

	ArrayMesh transformedBottomSide;
	bool IsInitialized => !transformedStairs[0].IsEmpty;

	bool IsSetup =>
		stairs != null &&
		innerCorner != null &&
		outerCorner != null &&
		rightSide != null &&
		leftSide != null &&
		backSide != null &&
		bottomSide != null;

	protected override void ValidateInternal()
	{
		RegenerateMatrixDictionary();
	}

	void RegenerateMatrixDictionary()
	{
		if (!IsSetup) return;

		Matrix4x4 baseMatrix = autoConvertFromRightHanded ? rightToLeftHanded : Matrix4x4.identity;
		transformedBottomSide = ArrayMesh.CreateFromMesh(bottomSide, baseMatrix);

		for (int i = 0; i < 4; i++)
		{
			int rotation = i * 90;
			Quaternion rotationQ = Quaternion.Euler(0, rotation, 0);
			Matrix4x4 transformation = Matrix4x4.Rotate(rotationQ) * baseMatrix;

			transformedStairs[i] = ArrayMesh.CreateFromMesh(stairs, transformation);
			transformedInnerCorners[i] = ArrayMesh.CreateFromMesh(innerCorner, transformation);
			transformedOuterCorner[i] = ArrayMesh.CreateFromMesh(outerCorner, transformation);
			transformedRightSide[i] = ArrayMesh.CreateFromMesh(rightSide, transformation);
			transformedLeftSide[i] = ArrayMesh.CreateFromMesh(leftSide, transformation);
			transformedBackSide[i] = ArrayMesh.CreateFromMesh(backSide, transformation);
		}
	}


	protected sealed override void GenerateMeshData(
		VoxelMap map,
		List<Vector3Int> voxelPositions,
		int shapeIndex,
		MeshBuilder meshBuilder)
	{
		if (!IsInitialized)
			RegenerateMatrixDictionary();

		if (!IsInitialized) return;
		if (!IsSetup) return;

		for (int i = 0; i < voxelPositions.Count; i++)
			SetupVoxelTypeAndRotation(map, voxelPositions[i], shapeIndex);

		for (int i = 0; i < voxelPositions.Count; i++)
			BuildMesh(map, voxelPositions[i], meshBuilder);
	}

	static readonly Vector3 half = Vector3.one * 0.5f;
	static readonly Vector3Int[] neighbourDirections =
	{
		Vector3Int.forward,
		Vector3Int.right,
		Vector3Int.back,
		Vector3Int.left
	};
	static readonly Vector3Int[] diagonalDirections =
	{
		new (1,0,1),
		new (1,0,-1),
		new (-1,0,-1),
		new (-1,0,1),
	};

	enum StairType
	{
		Simple = stairType_Simple,
		InnerCorner = stairType_InnerCorner,
		OuterCorner = stairType_OuterCorner,
		WalledIn = stairType_WalledIn,
	}

	const int stairType_Simple = 0;
	const int stairType_InnerCorner = 1;
	const int stairType_OuterCorner = 2;
	const int stairType_WalledIn = 3;

	const int extraInfo_isAutoSet = 0;
	const int extraInfo_stairType = 1;
	const int extraInfo_rotation = 2;
	const int extraInfo_level = 3;

	void SetupVoxelTypeAndRotation(VoxelMap map, Vector3Int position, int shapeIndex)
	{
		int voxelData = map.GetVoxel(position);
		ushort extraVoxelData = voxelData.GetExtraVoxelData();
		bool useAutoSetup = extraVoxelData.Get2Bit(extraInfo_isAutoSet) == 0;
		if (!useAutoSetup) return;

		// Setup Rotation & StairShape
		int wallNeighbourCount = 0;
		int stairNeighbourCount = 0;

		Vector3Int wallNeighbourDir = default;
		Vector3Int stairNeighbourDir = default;
		foreach (Vector3Int direction in neighbourDirections)
		{
			if (map.TryGetVoxel(position + direction, out int neighbour))
			{
				if (neighbour.GetShapeIndex() == shapeIndex)
				{
					stairNeighbourCount++;
					stairNeighbourDir += direction;
				}
				else if (neighbour.IsFilled())
				{
					wallNeighbourCount++;
					wallNeighbourDir += direction;
				}
			}
		}

		bool isEncircled = (wallNeighbourCount + stairNeighbourCount) == 4;
		if (isEncircled)
		{
			bool isFullyWalledIn =
				map.IsFilledSafe(position + Vector3Int.up) &&
				map.IsFilledSafe(position + Vector3Int.down);

			if (isFullyWalledIn)
			{
				extraVoxelData.Set2Bit(extraInfo_stairType, isFullyWalledIn ? stairType_WalledIn : stairType_Simple);
				map.SetVoxel(position, voxelData);
				return;
			}
		}

		int stairType = extraVoxelData.Get2Bit(extraInfo_stairType);
		int stairRotation = extraVoxelData.Get2Bit(extraInfo_rotation);
		// int stairLevel = extraVoxelData.Get2Bit(extraInfo_level);

		if (wallNeighbourCount == 0)
		{
			Vector3Int diagonalDir = Vector3Int.zero;
			foreach (Vector3Int diagonal in diagonalDirections)
			{
				if (map.TryGetVoxel(position + diagonal, out int neighbour) &&
					neighbour.GetShapeIndex() != shapeIndex && neighbour.IsFilled())
				{
					diagonalDir = diagonal;
					break;
				}
			} 

			if (diagonalDir!= Vector3Int.zero)
			{
				stairType = stairType_OuterCorner;
				stairRotation = GetRotation(stairRotation, diagonalDir);
			}
			else
				stairType = stairType_Simple;
		}

		else if (wallNeighbourCount == 1)
		{
			stairType = stairType_Simple;
			stairRotation = GetRotation(stairRotation, wallNeighbourDir);
		}
		else if (wallNeighbourCount == 2)
		{
			if (stairNeighbourCount == 2)
			{
				stairType = stairType_InnerCorner;
				stairRotation = GetRotation(stairRotation, wallNeighbourDir);
			}
			else
			{
				stairType = stairType_Simple;
				stairRotation = GetRotation(stairRotation, wallNeighbourDir + stairNeighbourDir);
			}
		}
		else if (wallNeighbourCount == 3)
		{
			stairType = stairType_Simple;
			stairRotation = GetRotation(stairRotation, wallNeighbourDir);
		}
		else //  wallNeighbourCount == 4 
		{

		}

		extraVoxelData.Set2Bit(extraInfo_stairType, stairType);
		extraVoxelData.Set2Bit(extraInfo_rotation, stairRotation);
		extraVoxelData.Set2Bit(extraInfo_level, 0); // TODO
		voxelData.SetExtraVoxelData(extraVoxelData);
		map.SetVoxel(position, voxelData);
	}

	void BuildMesh(VoxelMap map, Vector3Int position, MeshBuilder meshBuilder)
	{
		int voxelData = map.GetVoxel(position);
		ushort extraVoxelData = voxelData.GetExtraVoxelData();
		int stairType = extraVoxelData.Get2Bit(extraInfo_stairType);

		if (stairType == stairType_Simple)
			BuildSimpleStair(map, position, voxelData, meshBuilder);
		else if (stairType == stairType_InnerCorner)
			BuildCorner(map, position, voxelData, true, meshBuilder);
		else if (stairType == stairType_OuterCorner)
			BuildCorner(map, position, voxelData, false, meshBuilder);

	}

	void BuildSimpleStair(VoxelMap map, Vector3Int position, int voxelData, MeshBuilder meshBuilder)
	{
		ushort extraVoxelData = voxelData.GetExtraVoxelData();
		int rotation = extraVoxelData.Get2Bit(extraInfo_rotation);
		// int level = extraVoxelData.Get2Bit(extraInfo_level);

		Vector3 center = position + half;
		Vector3Int back = GetSimpleDirection(rotation);
		Vector3Int forward = -back;
		Vector3Int right = GetSimpleDirection((rotation + 1) % 4);
		Vector3Int left = -right;

		bool forwardFilled = map.IsFilledSafe(position + forward);
		bool upFilled = map.IsFilledSafe(position + Vector3Int.up);
		bool downFilled = map.IsFilledSafe(position + Vector3Int.down);
		bool backFilled = map.IsFilledSafe(position + back);
		bool isRightFilled = map.IsFilledSafe(position + right);
		bool isLeftFilled = map.IsFilledSafe(position + left);
		bool isOpen = !forwardFilled || !upFilled;

		if (isOpen)
		{
			meshBuilder.Add(transformedStairs[rotation], center);
		}

		if (!downFilled)
			meshBuilder.Add(transformedBottomSide, center);

		if (!backFilled)
			meshBuilder.Add(transformedBackSide[rotation], center);

		if (!isRightFilled)
		{
			if (isOpen)
				meshBuilder.Add(transformedRightSide[rotation], center);
			else
				meshBuilder.Add(transformedBackSide[(rotation + 1) % 4], center);
		}

		if (!isLeftFilled)
		{
			if (isOpen)
			{
				meshBuilder.Add(transformedLeftSide[rotation], center);
			}
			else
				meshBuilder.Add(transformedBackSide[MathHelper.Mod(rotation - 1, 4)], center);
		}
	}

	void BuildCorner(VoxelMap map, Vector3Int position, int voxelData, bool isInner, MeshBuilder meshBuilder)
	{
		ushort extraVoxelData = voxelData.GetExtraVoxelData();
		int rotation = extraVoxelData.Get2Bit(extraInfo_rotation);
		// int level = extraVoxelData.Get2Bit(extraInfo_level);
		Vector3 center = position + half;
		Vector3Int stairUpward = GetCornerDirection(rotation);

		bool downFilled = map.IsFilledSafe(position + Vector3Int.down);
		if (!downFilled)
			meshBuilder.Add(transformedBottomSide, center);

		if (isInner)
		{
			meshBuilder.Add(transformedInnerCorners[rotation], center);

			Vector3Int left = -ToRightOfCorner(stairUpward);
			Vector3Int right = -stairUpward - left;
			// Debug.Log("forward" + stairUpward + " left: " + left + " right: " + right);

			bool isRightSideFilled = map.IsFilledSafe(position + right);
			bool isLeftSideFilled = map.IsFilledSafe(position + left);

			if (!isRightSideFilled)
				meshBuilder.Add(transformedRightSide[rotation], center);
			if (!isLeftSideFilled)
				meshBuilder.Add(transformedLeftSide[MathHelper.Mod(rotation - 1, 4)], center);
		}
		else
		{ 
			meshBuilder.Add(transformedOuterCorner[rotation], center);

			bool isSide1Filled = map.IsFilledSafe(position + new Vector3Int(stairUpward.x, 0, 0));
			bool is2ide2Filled = map.IsFilledSafe(position + new Vector3Int(0, 0, stairUpward.z));
			if (!isSide1Filled)
				meshBuilder.Add(transformedRightSide[rotation], center);
			if (!is2ide2Filled)
				meshBuilder.Add(transformedLeftSide[rotation], center);
		}

	}

	public override bool IsSideFilled(GeneralDirection3D dir) => false; // TODO

	// ROTATION HELPER METHODSS ------------

	// SIMPLE STAIR ROTATIONS
	// stairRotation index == 0 -> stair raises in forward diagonal
	// stairRotation index == 1 -> stair raises in left diagonal
	// stairRotation index == 2 -> stair raises in back diagonal
	// stairRotation index == 3 -> stair raises in right diagonal

	// CORNER STAIR ROTATIONS
	// stairRotation index == 0 -> stair raises in forward-right diagonal
	// stairRotation index == 1 -> stair raises in forward-left diagonal
	// stairRotation index == 2 -> stair raises in back-left diagonal
	// stairRotation index == 3 -> stair raises in back-right diagonal

	int GetRotation(int initialRotation, Vector3Int upDirection)
	{
		int upX = upDirection.x;
		int upZ = upDirection.z;

		if (upX == 0 && upZ == 0) return initialRotation;

		if (upX == 0 || upZ == 0)  // Simple
		{
			if (upZ == 1) return 0; // Forward
			if (upX == 1) return 1; // Right
			if (upZ == -1) return 2; // Back
			if (upX == -1) return 3; // Left
		}

		if (upZ == 1)  // Corner
		{
			if (upX == 1) return 1; // Forward - Right
			if (upX == -1) return 0; // Forward - Left
		}
		else
		{
			if (upX == 1) return 2; // Back - Right
			if (upX == -1) return 3; // Left - Left
		}

		return initialRotation;
	}

	Vector3Int GetSimpleDirection(int rotation) => rotation switch
	{
		0 => Vector3Int.forward,
		1 => Vector3Int.right,
		2 => Vector3Int.back,
		3 => Vector3Int.left,
		_ => throw new NotImplementedException()
	};

	Vector3Int GetCornerDirection(int rotation) => rotation switch
	{
		0 => new Vector3Int(-1, 0, 1),
		1 => new Vector3Int(1, 0, 1),
		2 => new Vector3Int(1, 0, -1),
		3 => new Vector3Int(-1, 0, -1),
		_ => throw new NotImplementedException()
	};

	Vector3Int ToRightOfCorner(Vector3Int upDirection)
	{
		int upX = upDirection.x;
		int upZ = upDirection.z;

		if (upX == 1)
		{
			if (upZ == 1)  // Forward - Right
				return new Vector3Int(1, 0, 0);
			else           // Back - Right
				return new Vector3Int(0, 0, -1);
		}
		else
		{
			if (upZ == 1)  // Forward - Let
				return new Vector3Int(0, 0, 1);
			else           // Back - Let
				return new Vector3Int(-1, 0, 0);
		}
	}

	List<ExtraControl> controls;
	public override IReadOnlyList<ExtraControl> GetExtraControls()
	{
		controls ??= new List<ExtraControl>()
		{
			new ExtraControl<bool> ()
			{
				name = "Enable Auto Setup",
				getValue = GetAutoSetup,
				setValue = SetAutoSetup
			},
			new ExtraControl<StairType>()
			{
				name = "Stair Type",
				getValue = GetStairType,
				setValue = SetStairType
			},
			new ExtraControl<int>
			{
				name = "Rotation",
				getValue = GetRotation,
				setValue = SetRotation
			},
			new ExtraControl<int>
			{
				name = "Level",
				getValue = GetLevel,
				setValue = SetLevel
			}
		};
		return controls;
	}

	static bool GetAutoSetup(ushort extraVoxelData) => extraVoxelData.Get2Bit(extraInfo_isAutoSet) == 1;
	static ushort SetAutoSetup(ushort originalExtraVoxelData, bool newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_isAutoSet, newValue ? 0 : 1);
	static StairType GetStairType(ushort voxelData) => (StairType)voxelData.Get2Bit(extraInfo_stairType);
	static ushort SetStairType(ushort originalExtraVoxelData, StairType newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_stairType, (int)newValue);
	static int GetRotation(ushort extraVoxelData) => 
		extraVoxelData.Get2Bit(extraInfo_rotation);
	static ushort SetRotation(ushort originalExtraVoxelData, int newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_rotation, newValue);
	static int GetLevel(ushort extraVoxelData) => extraVoxelData.Get2Bit(extraInfo_level);
	static ushort SetLevel(ushort originalExtraVoxelData, int newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_level, newValue);
}