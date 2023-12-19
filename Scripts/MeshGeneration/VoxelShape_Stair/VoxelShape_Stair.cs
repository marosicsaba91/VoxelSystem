using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[CreateAssetMenu(fileName = "StairVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: Stair", order = EditorConstants.soOrder_VoxelShape)]
public class VoxelShape_Stair : VoxelShapeBuilder
{
	[Header("Custom Meshes")]

	[SerializeField, Tooltip("The stair goes up looking FORWARD\n")]
	Mesh stairs;
	[SerializeField, Tooltip("The corners of the stair looking LEFT-FORWARD\n")]
	Mesh innerCorner;
	[SerializeField, Tooltip("The corners of the stair looking LEFT-FORWARD\n")]
	Mesh outerCorner;
	[SerializeField, Tooltip("The side of the stair that goes up looking FORWARD")]
	Mesh rightSide;
	[SerializeField, Tooltip("The side of the stair that goes up looking FORWARD")]
	Mesh leftSide;
	[SerializeField, Tooltip("The back of the Mesh looking FORWARD")]
	Mesh backSide;
	[SerializeField, Tooltip("The back of the Mesh looking Down Globally")]
	Mesh bottomSide;

	[SerializeField] bool autoConvertFromRightHanded = true;


	[Header("Texture")]
	[SerializeField] CubeUVSetup cubeTextureCoordinates;
	[SerializeField] bool useTextureSettingOnCustomMeshes = true;

	[Header("Other SetupFromMesh")]
	// [SerializeField, Range(1, 4)] int slope = 1;
	[SerializeField] bool isTransparent = false;


	[SerializeField, HideInInspector] MeshBuilder[] transformedStairs = new MeshBuilder[4];
	[SerializeField, HideInInspector] MeshBuilder[] transformedInnerCorners = new MeshBuilder[4];
	[SerializeField, HideInInspector] MeshBuilder[] transformedOuterCorner = new MeshBuilder[4];
	[SerializeField, HideInInspector] MeshBuilder[] transformedRightSide = new MeshBuilder[4];
	[SerializeField, HideInInspector] MeshBuilder[] transformedLeftSide = new MeshBuilder[4];
	[SerializeField, HideInInspector] MeshBuilder[] transformedBackSide = new MeshBuilder[4];
	[SerializeField, HideInInspector] MeshBuilder transformedBottomSide;

	protected sealed override bool IsInitialized => !transformedStairs[0].IsEmpty;

	protected override void InitializeMeshCache()
	{
		transformedBottomSide = GenerateBottom();

		for (int i = 0; i < 4; i++)
		{
			int rotation = i * 90;
			Quaternion rotationQ = Quaternion.Euler(0, rotation, 0);
			Matrix4x4 transformation = Matrix4x4.Rotate(rotationQ);

			// TODO: Create parts automatically if not set up directly

			transformedStairs[i] = GenerateStair();
			transformedStairs[i].Transform(transformation);
			transformedInnerCorners[i] = GenerateInnerCorners();
			transformedInnerCorners[i].Transform(transformation);
			transformedOuterCorner[i] = GenerateOuterCorners();
			transformedOuterCorner[i].Transform(transformation);
			transformedRightSide[i] = GenerateSide(HorizontalDirection.Right);
			transformedRightSide[i].Transform(transformation);
			transformedLeftSide[i] = GenerateSide(HorizontalDirection.Left);
			transformedLeftSide[i].Transform(transformation);
			transformedBackSide[i] = GenerateBack();
			transformedBackSide[i].Transform(transformation);
		}
	}

	MeshBuilder GenerateBottom()
	{
		if (bottomSide != null)
		{
			MeshBuilder result = new (bottomSide, autoConvertFromRightHanded);
			if (useTextureSettingOnCustomMeshes)
				result.ProjectUV(cubeTextureCoordinates.GetRect(GeneralDirection3D.Down), Axis3D.Y);
			return result;
		}
		return VoxelShape_Cube.GenerateDefaultSide(GeneralDirection3D.Down, cubeTextureCoordinates);
	}

	MeshBuilder GenerateStair()
	{
		if (stairs != null)
		{
			MeshBuilder result = new (stairs, autoConvertFromRightHanded);
			if (useTextureSettingOnCustomMeshes)
				result.ProjectUV(cubeTextureCoordinates.GetRect(GeneralDirection3D.Up), Axis3D.Y);
			return result;
		}
		Vector3 normal = new(0, 1, -1);

		Rect rect = cubeTextureCoordinates.GetRect(GeneralDirection3D.Up);

		List<Vector3> vs = new(){ new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), };
		List<Vector3> ns = new() { normal, normal, normal, normal };
		List<Vector2> uvs = new() { rect.BottomLeft(), rect.TopLeft(), rect.TopRight(), rect.BottomRight() };
		List<int> triangles = new() { 0, 2, 1, 0, 3, 2 };

		return new()
		{
			vertices = vs,
			normals = ns,
			uv = uvs,
			triangles = triangles
		};
	}

	MeshBuilder GenerateInnerCorners()
	{
		if (innerCorner != null)
		{
			MeshBuilder result = new(innerCorner, autoConvertFromRightHanded);
			if (useTextureSettingOnCustomMeshes)
				result.ProjectUV(cubeTextureCoordinates.GetRect(GeneralDirection3D.Up), Axis3D.Y);
			return result;
		}
		Rect rect = cubeTextureCoordinates.GetRect(GeneralDirection3D.Up);
		Vector3 normal1 = new(0, 1, -1);  // Right Side
		Vector3 normal2 = new(1, 1, 0);


		List<Vector3> vs = new()  {
			new(0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f),
			new(0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, 0.5f), };
		List<Vector3> ns = new() { normal1, normal1, normal1, normal2, normal2, normal2 };
		List<Vector2> uvs = new() { rect.BottomLeft(), rect.TopRight(), rect.TopLeft(), rect.BottomLeft(), rect.BottomRight(), rect.TopRight(), };
		List<int> triangles = new() { 0, 1, 2, 3, 4, 5 };

		return new()
		{
			vertices = vs,
			normals = ns,
			uv = uvs,
			triangles = triangles
		};
	}

	MeshBuilder GenerateOuterCorners()
	{
		if (outerCorner != null)
		{
			MeshBuilder result = new (outerCorner, autoConvertFromRightHanded);
			if (useTextureSettingOnCustomMeshes)
				result.ProjectUV(cubeTextureCoordinates.GetRect(GeneralDirection3D.Up), Axis3D.Y);
			return result;
		}
		Rect rect = cubeTextureCoordinates.GetRect(GeneralDirection3D.Up);
		Vector3 normal1 = new(1, 1, 0);  // Right Side
		Vector3 normal2 = new(0, 1, -1);

		List<Vector3> vs = new() {
			new(0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, 0.5f), new(0.5f, -0.5f, 0.5f),
			new(0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, 0.5f), };
		List<Vector3> ns = new() { normal1, normal1, normal1, normal2, normal2, normal2 };
		List<Vector2> uvs = new() { rect.BottomLeft(), rect.TopRight(), rect.TopLeft(), rect.BottomLeft(), rect.BottomRight(), rect.TopRight(), };
		List<int> triangles = new() { 0, 1, 2, 3, 4, 5 };

		return new()
		{
			vertices = vs,
			normals = ns,
			uv = uvs,
			triangles = triangles
		};
	}

	MeshBuilder GenerateSide(HorizontalDirection direction)
	{
		if (direction == HorizontalDirection.Left && leftSide != null)
		{
			MeshBuilder result = new(leftSide, autoConvertFromRightHanded);
			if (useTextureSettingOnCustomMeshes)
				result.ProjectUV(cubeTextureCoordinates.GetRect(GeneralDirection3D.Left), Axis3D.X);
			return result;
		}
		if (direction == HorizontalDirection.Right && rightSide != null)
		{
			MeshBuilder result = new(rightSide, autoConvertFromRightHanded);
			result.ProjectUV(cubeTextureCoordinates.GetRect(GeneralDirection3D.Right), Axis3D.X);
			return result;
		}
		GeneralDirection3D dir3D = direction.ToGeneralDirection3D();
		Vector3 normal = dir3D.ToVector();
		Vector3 offset = dir3D.ToVector() * 0.5f;

		Rect rect = cubeTextureCoordinates.GetRect(GeneralDirection3D.Up);
		List<Vector3> vs = new (){ new Vector3(0, -0.5f, -0.5f) + offset, new Vector3(0, -0.5f, 0.5f) + offset, new Vector3(0, 0.5f, 0.5f) + offset };
		List<Vector3> ns = new() { normal, normal, normal, normal };
		List<Vector2> uvs = new() { rect.BottomLeft(), rect.BottomRight(), rect.TopRight(), };
		List<int> triangles = dir3D == GeneralDirection3D.Left ? new() { 0, 1, 2 } : new() { 2, 1, 0 };

		return new()
		{
			vertices = vs,
			normals = ns,
			uv = uvs,
			triangles = triangles
		};
	}

	MeshBuilder GenerateBack()
	{
		if (backSide != null)
		{
			MeshBuilder result = new(backSide, autoConvertFromRightHanded);
			if (useTextureSettingOnCustomMeshes)
				result.ProjectUV(cubeTextureCoordinates.GetRect(GeneralDirection3D.Back), Axis3D.Z);
			return result;
		}
		return VoxelShape_Cube.GenerateDefaultSide(GeneralDirection3D.Forward, cubeTextureCoordinates);
	}

	protected sealed override void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions)
	{
		for (int i = 0; i < voxelPositions.Count; i++)
		{
			Vector3Int voxelPosition = voxelPositions[i];
			Voxel v = map.GetVoxel(voxelPosition);

			ShapeType stairType = GetStairType(v.extraVoxelData);

			if (isTransparent || stairType == ShapeType.FullBlock)
				v.OpenAllSide();
			else
			{
				int rotation = GetRotation(v.extraVoxelData);

				CubicTransformation transformation = new(GeneralDirection3D.Up, rotation, false);
				v.SetSideClosed(GeneralDirection3D.Up, false);
				v.SetSideClosed(GeneralDirection3D.Down, true);

				GeneralDirection3D globalRight = transformation.TransformDirection(GeneralDirection3D.Right);
				GeneralDirection3D globalLeft = transformation.TransformDirection(GeneralDirection3D.Left);
				GeneralDirection3D globalForward = transformation.TransformDirection(GeneralDirection3D.Forward);
				GeneralDirection3D globalBack = transformation.TransformDirection(GeneralDirection3D.Back);

				v.SetSideClosed(globalRight, false);
				v.SetSideClosed(globalBack, false);

				bool leftClosed = stairType == ShapeType.InnerCornerStair;
				bool forwardClosed = stairType is ShapeType.SimpleStair or ShapeType.InnerCornerStair;

				v.SetSideClosed(globalLeft, leftClosed);
				v.SetSideClosed(globalForward, forwardClosed);
			}

			map.SetVoxel(voxelPosition, v);
		}
	}

	protected sealed override void GenerateMeshData(
		VoxelMap map,
		List<Vector3Int> voxelPositions,
		int shapeIndex,
		MeshBuilder meshBuilder)
	{
		for (int i = 0; i < voxelPositions.Count; i++)
			BuildMesh(map, voxelPositions[i], meshBuilder);
	}

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

	GeneralDirection3D GetStairRightSideGlobal(ushort extraData) => GetStairType(extraData) switch
	{ 
		ShapeType.OuterCornerStair => GetSimpleDirections(GetRotation(extraData)),
		_ => GetSimpleDirections((GetRotation(extraData) + 1) % 4)
	};

	GeneralDirection3D GetStairLeftSideGlobal(ushort extraData) => GetStairType(extraData) switch
	{ 
		ShapeType.InnerCornerStair => GetSimpleDirections((GetRotation(extraData) + 2) % 4),
		_ => GetSimpleDirections((GetRotation(extraData) + 3) % 4)
	};

	const int extraInfo_isAutoSet = 0;
	const int extraInfo_stairType = 1;
	const int extraInfo_rotation = 2;
	const int extraInfo_level = 3;

	protected override void SetupVoxelData(VoxelMap map, List<Vector3Int> voxelPositions, int shapeIndex)
	{
		for (int i = 0; i < voxelPositions.Count; i++)
			SetupVoxelTypeAndRotation(map, voxelPositions[i], shapeIndex);
	}

	void SetupVoxelTypeAndRotation(VoxelMap map, Vector3Int position, int shapeIndex)
	{
		Voxel voxelData = map.GetVoxel(position);
		ushort extraVoxelData = voxelData.extraVoxelData;
		bool useAutoSetup = GetAutoSetup(extraVoxelData);
		if (!useAutoSetup) return;

		// SetupFromMesh Rotation & StairShape
		int wallNeighbourCount = 0;
		int stairNeighbourCount = 0;

		Vector3Int wallNeighbourDir = default;
		Vector3Int stairNeighbourDir = default;
		foreach (Vector3Int direction in neighbourDirections)
		{
			if (map.TryGetVoxel(position + direction, out Voxel neighbour))
			{
				if (neighbour.shapeId == shapeIndex)
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
				extraVoxelData.Set2Bit(extraInfo_stairType, isFullyWalledIn ? (int)ShapeType.FullBlock : (int)ShapeType.SimpleStair);
				map.SetVoxel(position, voxelData);
				return;
			}
		}

		int stairType = extraVoxelData.Get2Bit(extraInfo_stairType);
		int stairRotation = extraVoxelData.Get2Bit(extraInfo_rotation);
		// int currentLevel = extraVoxelData.Get2Bit(extraInfo_level);

		if (wallNeighbourCount == 0)
		{
			Vector3Int diagonalDir = Vector3Int.zero;
			foreach (Vector3Int diagonal in diagonalDirections)
			{
				if (map.TryGetVoxel(position + diagonal, out Voxel neighbour) &&
					neighbour.shapeId != shapeIndex && neighbour.IsFilled())
				{
					diagonalDir = diagonal;
					break;
				}
			}

			if (diagonalDir != Vector3Int.zero)
			{
				stairType = (int)ShapeType.OuterCornerStair;
				stairRotation = GetRotation(stairRotation, diagonalDir);
			}
			else
				stairType = (int)ShapeType.SimpleStair;
		}

		else if (wallNeighbourCount == 1)
		{
			stairType = (int)ShapeType.SimpleStair;
			stairRotation = GetRotation(stairRotation, wallNeighbourDir);
		}
		else if (wallNeighbourCount == 2)
		{
			if (stairNeighbourCount == 2)
			{
				stairType = (int)ShapeType.InnerCornerStair;
				stairRotation = GetRotation(stairRotation, wallNeighbourDir);
			}
			else
			{
				stairType = (int)ShapeType.SimpleStair;
				stairRotation = GetRotation(stairRotation, wallNeighbourDir + stairNeighbourDir);
			}
		}
		else if (wallNeighbourCount == 3)
		{
			stairType = (int)ShapeType.SimpleStair;
			stairRotation = GetRotation(stairRotation, wallNeighbourDir);
		}

		extraVoxelData.Set2Bit(extraInfo_stairType, stairType);
		extraVoxelData.Set2Bit(extraInfo_rotation, stairRotation);
		extraVoxelData.Set2Bit(extraInfo_level, 0); // TODO
		voxelData.extraVoxelData = extraVoxelData;
		map.SetVoxel(position, voxelData);
	}

	void BuildMesh(VoxelMap map, Vector3Int position, MeshBuilder meshBuilder)
	{
		Voxel voxelData = map.GetVoxel(position);
		ushort extraVoxelData = voxelData.extraVoxelData;
		ShapeType stairType = (ShapeType) extraVoxelData.Get2Bit(extraInfo_stairType);

		switch (stairType)
		{
			case ShapeType.SimpleStair:
				BuildSimpleStair(map, position, voxelData, meshBuilder);
				break;
			case ShapeType.InnerCornerStair:
				BuildCorner(map, position, voxelData, true, meshBuilder);
				break;
			case ShapeType.OuterCornerStair:
				BuildCorner(map, position, voxelData, false, meshBuilder);
				break;
			case ShapeType.FullBlock:
				// Nothing To Do
				break;
		}
	}

	void BuildSimpleStair(VoxelMap map, Vector3Int position, Voxel voxelData, MeshBuilder meshBuilder)
	{
		ushort extraVoxelData = voxelData.extraVoxelData;
		int rotation = extraVoxelData.Get2Bit(extraInfo_rotation);
		int level = extraVoxelData.Get2Bit(extraInfo_level);
		bool autoSet = GetAutoSetup(extraVoxelData);

		// int offset = Mathf.Min(level, slope - 1) - 	
		// Vector3 levelOffset = Mathf.Min(level, slope-1) / slope / 2 * Vector3.up;
		// Debug.Log("levelOffset: " + levelOffset);

		Vector3 center = position + half;

		GeneralDirection3D back = GetSimpleDirections(rotation);
		GeneralDirection3D forward = back.Opposite();
		GeneralDirection3D right = GetSimpleDirections((rotation + 1) % 4);
		GeneralDirection3D left = right.Opposite();

		bool forwardFilled = map.IsFilledSafe(position + forward.ToVectorInt(), forward);
		bool upFilled = map.IsFilledSafe(position + Vector3Int.up, GeneralDirection3D.Up);

		bool downFilled = map.IsFilledSafe(position + Vector3Int.down, GeneralDirection3D.Down);
		bool backFilled = map.IsFilledSafe(position + back.ToVectorInt(), forward);
		if (!downFilled)
			meshBuilder.Add(transformedBottomSide, center);

		if (!backFilled) 
			meshBuilder.Add(transformedBackSide[rotation], center);

		bool addStair = !forwardFilled || !upFilled;

		if (addStair)
		{
			meshBuilder.Add(transformedStairs[rotation], center);
		}

		// SIDE MESHES

		bool closeRight = true;
		bool closeLeft = true;
		 
		if (map.TryGetVoxel(position + right.ToVectorInt(), out Voxel rightNeighbour))
		{
			if (autoSet && rightNeighbour.shapeId == voxelData.shapeId)
				closeRight = false;
			else
				closeRight = !rightNeighbour.IsFilled(left);
		}

		if (map.TryGetVoxel(position + left.ToVectorInt(), out Voxel leftNeighbour))
		{
			if (autoSet && leftNeighbour.shapeId == voxelData.shapeId)
				closeLeft = false;
			else
				closeLeft = !leftNeighbour.IsFilled(right);
		}

		if (closeRight)
		{
			if (addStair)
				meshBuilder.Add(transformedRightSide[rotation], center);
			else
				meshBuilder.Add(transformedBackSide[(rotation + 1) % 4], center);
		}

		if (closeLeft)
		{
			if (addStair)
				meshBuilder.Add(transformedLeftSide[rotation], center);
			else
				meshBuilder.Add(transformedBackSide[(rotation +3) % 4], center);
		}
	}

	void BuildCorner(VoxelMap map, Vector3Int position, Voxel voxelData, bool isInner, MeshBuilder meshBuilder)
	{
		ushort extraVoxelData = voxelData.extraVoxelData;
		int rotation = extraVoxelData.Get2Bit(extraInfo_rotation);
		bool autoSet = GetAutoSetup(extraVoxelData);
		// int level = extraVoxelData.Get2Bit(extraInfo_level);
		Vector3 center = position + half;
		
		// TOP MESH
		if (isInner)
			meshBuilder.Add(transformedInnerCorners[rotation], center);
		else
			meshBuilder.Add(transformedOuterCorner[rotation], center);

		// BOTTOM MESH

		bool downFilled = map.IsFilledSafe(position + Vector3Int.down, GeneralDirection3D.Down);
		if (!downFilled)
			meshBuilder.Add(transformedBottomSide, center);

		// SIDE MESHES

		Vector3Int stairUpward = GetCornerDirection(rotation);
		Vector3Int rightSideVector = ToRightOfCorner(stairUpward);
		Vector3Int leftSideVector = stairUpward - rightSideVector;
		GeneralDirection3D leftSideDirection = DirectionUtility.GeneralDirection3DFromVector(leftSideVector).Opposite();
		GeneralDirection3D rightSideDirection = DirectionUtility.GeneralDirection3DFromVector(rightSideVector).Opposite();
		 
		if (isInner) 
		{
			// FIX7
			GeneralDirection3D temp = leftSideDirection;
			leftSideDirection = rightSideDirection.Opposite();
			rightSideDirection = temp.Opposite();

			rightSideVector = rightSideDirection.Opposite().ToVectorInt();
			leftSideVector = leftSideDirection.Opposite().ToVectorInt();
		}

		bool closeLeftSide = true;
		if (map.TryGetVoxel(position + leftSideVector, out Voxel leftNeighbour))
		{
			if (autoSet && leftNeighbour.shapeId == voxelData.shapeId)
				closeLeftSide = false;
			else
				closeLeftSide = !leftNeighbour.IsFilled(leftSideDirection);
		}

		bool closeRightSide = true;
		if (map.TryGetVoxel(position + rightSideVector, out Voxel rightNeighbour))
		{
			if (autoSet && rightNeighbour.shapeId == voxelData.shapeId)
				closeRightSide = false;
			else
				closeRightSide = !rightNeighbour.IsFilled(rightSideDirection);
		}

		if (isInner)
		{
			if (closeLeftSide)
				meshBuilder.Add(transformedLeftSide[(rotation + 3) % 4], center);
			if (closeRightSide)
				meshBuilder.Add(transformedRightSide[(rotation ) % 4], center);
		}
		else
		{
			if (closeLeftSide)
				meshBuilder.Add(transformedLeftSide[rotation], center);
			if (closeRightSide)
				meshBuilder.Add(transformedRightSide[(rotation + 3) % 4], center);
		}

		// BACK MESHES

		if (isInner)
		{
			Vector3Int back1Vector = stairUpward + leftSideVector;
			Vector3Int back2Vector = stairUpward + rightSideVector;
			GeneralDirection3D back1Direction = DirectionUtility.GeneralDirection3DFromVector(back1Vector).Opposite();
			GeneralDirection3D back2Direction = DirectionUtility.GeneralDirection3DFromVector(back2Vector).Opposite();

			bool isBack2Filled = map.IsFilledSafe(position + back1Vector, back1Direction);
			bool isBack1Filled = map.IsFilledSafe(position + back2Vector, back2Direction);
			if (!isBack1Filled)
				meshBuilder.Add(transformedBackSide[(rotation + 0) % 4], center);
			if (!isBack2Filled)
				meshBuilder.Add(transformedBackSide[(rotation + 3) % 4], center);
		}
	}

	// ROTATION HELPER METHODSS ------------

	// SIMPLE STAIR ROTATIONS
	// stairRotation index == 0 -> stair raises in forward diagonal
	// stairRotation index == 1 -> stair raises in leftSideVector diagonal
	// stairRotation index == 2 -> stair raises in back diagonal
	// stairRotation index == 3 -> stair raises in rightSideVector diagonal

	// CORNER STAIR ROTATIONS
	// stairRotation index == 0 -> stair raises in forward-rightSideVector diagonal
	// stairRotation index == 1 -> stair raises in forward-leftSideVector diagonal
	// stairRotation index == 2 -> stair raises in back-leftSideVector diagonal
	// stairRotation index == 3 -> stair raises in back-rightSideVector diagonal

	int GetRotation(int initialRotation, Vector3Int upDirection)
	{
		int upX = upDirection.x;
		int upZ = upDirection.z;

		if (upX == 0 && upZ == 0) return initialRotation;

		if (upX == 0 || upZ == 0)  // SimpleStair
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
	 
	GeneralDirection3D GetSimpleDirections(int rotation) => rotation switch
	{
		0 => GeneralDirection3D.Forward,
		1 => GeneralDirection3D.Right,
		2 => GeneralDirection3D.Back,
		3 => GeneralDirection3D.Left,
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

	List<ExtraVoxelControl> controls;
	public override IReadOnlyList<ExtraVoxelControl> GetExtraControls()
	{
		controls ??= new List<ExtraVoxelControl>()
		{
			new ExtraVoxelControl<bool> ()
			{
				name = "Enable Auto SetupFromMesh",
				getValue = GetAutoSetup,
				setValue = SetAutoSetup
			},
			new ExtraVoxelControl<ShapeType>()
			{
				name = "Stair Type",
				getValue = GetStairType,
				setValue = SetStairType
			},
			new ExtraVoxelControl<int>
			{
				name = "Rotation",
				getValue = GetRotation,
				setValue = SetRotation
			},
			new ExtraVoxelControl<int>
			{
				name = "Level",
				getValue = GetLevel,
				setValue = SetLevel
			}
		};
		return controls;
	}

	static bool GetAutoSetup(ushort extraVoxelData) => extraVoxelData.Get2Bit(extraInfo_isAutoSet) == 0;
	static ushort SetAutoSetup(ushort originalExtraVoxelData, bool newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_isAutoSet, newValue ? 0 : 1);
	static ShapeType GetStairType(ushort voxelData) => (ShapeType)voxelData.Get2Bit(extraInfo_stairType);
	static ushort SetStairType(ushort originalExtraVoxelData, ShapeType newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_stairType, (int)newValue);
	static int GetRotation(ushort extraVoxelData) =>
		extraVoxelData.Get2Bit(extraInfo_rotation);
	static ushort SetRotation(ushort originalExtraVoxelData, int newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_rotation, newValue);
	static int GetLevel(ushort extraVoxelData) => extraVoxelData.Get2Bit(extraInfo_level);
	static ushort SetLevel(ushort originalExtraVoxelData, int newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_level, newValue);



	protected override PhysicalVoxelShape PhysicalShape(ushort extraData)
	{
		GeneralDirection3D left = GetStairLeftSideGlobal(extraData);
		GeneralDirection3D right = GetStairRightSideGlobal(extraData);

		return new()
		{
			shapeType = GetStairType(extraData),

			stairSideUp = GeneralDirection3D.Up,
			stairSide1 = left,
			stairSide2 = right,
			
			levelLeight = 1,       // TODO
			levelCount = 1,        // TODO
			currentLevel = 0,        // TODO

			solidTop = true,
			solidBottom = true,
			solidForward = true,
			solidBack = true,
			solidLeft = true,
			solidRight = true,
		};
	}
}