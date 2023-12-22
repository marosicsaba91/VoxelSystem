using MeshUtility;
using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[System.Serializable]
struct EasySide
{
	public Vector3[] vertices;
}

[CreateAssetMenu(fileName = "StairVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: Stair", order = EditorConstants.soOrder_VoxelShape)]
public class VoxelShape_Stair : VoxelShapeBuilder
{

	// SIMPLE STAIR ROTATIONS
	// transformation index == 0 -> stair raises in forward diagonal
	// transformation index == 1 -> stair raises in leftSideVector diagonal
	// transformation index == 2 -> stair raises in back diagonal
	// transformation index == 3 -> stair raises in rightSideVector diagonal

	// CORNER STAIR ROTATIONS
	// transformation index == 0 -> stair raises in forward-rightSideVector diagonal
	// transformation index == 1 -> stair raises in forward-leftSideVector diagonal
	// transformation index == 2 -> stair raises in back-leftSideVector diagonal
	// transformation index == 3 -> stair raises in back-rightSideVector diagonal

	// INNER CORNER
	// RightSide -> right
	// LeftSide -> back
	// Back1 -> forward
	// Back2 -> left

	// OUTER CORNER
	// RightSide -> forward
	// LeftSide -> left


	[Header("Custom Meshes")]

	[SerializeField, Tooltip("The stair goes down looking FORWARD\n")]
	Mesh stairs;
	[SerializeField, Tooltip("The corners of the stair looking LEFT-FORWARD\n")]
	Mesh innerCorner;
	[SerializeField, Tooltip("The corners of the stair looking LEFT-FORWARD\n")]
	Mesh outerCorner;
	[SerializeField, Tooltip("The side of the stair that goes down looking FORWARD")]
	Mesh rightSide;
	[SerializeField, Tooltip("The side of the stair that goes down looking FORWARD")]
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




	// ----------------- Cached Data -------------------------------------

	static readonly Vector3[] bottomFull = { new(0, 0, 0), new(1, 0, 0), new(1, 0, 1), new(0, 0, 1) };
	static readonly Vector3[] forwardFull = { new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1) };
	static readonly Vector3[] ramp = { new(0, 0, 0), new(0, 1, 1), new(1, 1, 1), new(1, 0, 0) };
	const int transformCount = CubicTransformation.allTransformationCount;

	[SerializeField, HideInInspector] MeshBuilder[] transformedStairs = new MeshBuilder[transformCount];
	[SerializeField, HideInInspector] MeshBuilder[] transformedInnerCorners = new MeshBuilder[transformCount];
	[SerializeField, HideInInspector] MeshBuilder[] transformedOuterCorner = new MeshBuilder[transformCount];
	[SerializeField, HideInInspector] MeshBuilder[] transformedRightSide = new MeshBuilder[transformCount];
	[SerializeField, HideInInspector] MeshBuilder[] transformedLeftSide = new MeshBuilder[transformCount];
	[SerializeField, HideInInspector] MeshBuilder[] transformedBackSide = new MeshBuilder[transformCount];
	[SerializeField, HideInInspector] MeshBuilder[] transformedBottomSide = new MeshBuilder[transformCount];

	[SerializeField, HideInInspector] EasySide[] transformedStairs_Physical = new EasySide[transformCount];
	[SerializeField, HideInInspector] EasySide[] transformedInnerCorners_Physical = new EasySide[transformCount];
	[SerializeField, HideInInspector] EasySide[] transformedOuterCorner_Physical = new EasySide[transformCount];
	[SerializeField, HideInInspector] EasySide[] transformedRightSide_Physical = new EasySide[transformCount];
	[SerializeField, HideInInspector] EasySide[] transformedLeftSide_Physical = new EasySide[transformCount];
	[SerializeField, HideInInspector] EasySide[] transformedBackSide_Physical = new EasySide[transformCount];
	[SerializeField, HideInInspector] EasySide[] transformedBottomSide_Physical = new EasySide[transformCount];

	protected sealed override bool IsInitialized => !transformedStairs[0].IsEmpty;

	// ----------------- Initialize Cached Data -------------------------------------

	protected override void InitializeCachedData()
	{
		Setup(transformedStairs);
		Setup(transformedInnerCorners);
		Setup(transformedOuterCorner);
		Setup(transformedRightSide);
		Setup(transformedLeftSide);
		Setup(transformedBackSide);
		Setup(transformedBottomSide);
		static void Setup(MeshBuilder[] array)
		{
			if (array.Length != transformCount)
			{
				array = new MeshBuilder[transformCount];
				for (int i = 0; i < transformCount; i++)
				{
					array[i] = new MeshBuilder();
				}
			}
		}

		for (byte i = 0; i < CubicTransformation.allTransformationCount; i++)
		{
			CubicTransformation ct = new(i);
			Matrix4x4 transformation = ct.GetTransformationMatrix();

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
			transformedBottomSide[i] = GenerateBottom();
			transformedBottomSide[i].Transform(transformation);
		}
	}

	MeshBuilder GenerateBottom()
	{
		if (bottomSide != null)
		{
			MeshBuilder result = new(bottomSide, autoConvertFromRightHanded);
			if (useTextureSettingOnCustomMeshes)
				result.ProjectUV(cubeTextureCoordinates, GeneralDirection3D.Down);
			return result;
		}
		return cubeTextureCoordinates.GetCubeSide(GeneralDirection3D.Down);
	}

	MeshBuilder GenerateStair()
	{
		if (stairs != null)
		{
			MeshBuilder result = new(stairs, autoConvertFromRightHanded);
			if (useTextureSettingOnCustomMeshes)
				result.ProjectUV(cubeTextureCoordinates, GeneralDirection3D.Up);
			return result;
		}
		Vector3 normal = new(0, 1, -1);

		Rect rect = cubeTextureCoordinates.GetRect(GeneralDirection3D.Up);

		List<Vector3> vs = new() { new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), };
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
				result.ProjectUV(cubeTextureCoordinates, GeneralDirection3D.Up);
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
			MeshBuilder result = new(outerCorner, autoConvertFromRightHanded);
			if (useTextureSettingOnCustomMeshes)
				result.ProjectUV(cubeTextureCoordinates, GeneralDirection3D.Up);
			return result;
		}
		Rect rect = cubeTextureCoordinates.GetRect(GeneralDirection3D.Up);
		Vector3 normal1 = new(1, 1, 0);  // Right Side
		Vector3 normal2 = new(0, 1, -1);

		List<Vector3> vertices = new() {
			new(0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, 0.5f), new(0.5f, -0.5f, 0.5f),
			new(0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, 0.5f), };
		List<Vector3> normals = new() { normal1, normal1, normal1, normal2, normal2, normal2 };
		List<Vector2> uvs = new() { rect.BottomLeft(), rect.TopRight(), rect.TopLeft(), rect.BottomLeft(), rect.BottomRight(), rect.TopRight(), };
		List<int> triangles = new() { 0, 1, 2, 3, 4, 5 };

		return new()
		{
			vertices = vertices,
			normals = normals,
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
				result.ProjectUV(cubeTextureCoordinates, GeneralDirection3D.Left);
			return result;
		}
		if (direction == HorizontalDirection.Right && rightSide != null)
		{
			MeshBuilder result = new(rightSide, autoConvertFromRightHanded);
			result.ProjectUV(cubeTextureCoordinates, GeneralDirection3D.Right);
			return result;
		}
		GeneralDirection3D dir3D = direction.ToGeneralDirection3D();
		Vector3 normal = dir3D.ToVector();
		Vector3 offset = dir3D.ToVector() * 0.5f;

		Rect rect = cubeTextureCoordinates.GetRect(GeneralDirection3D.Up);
		List<Vector3> vs = new() { new Vector3(0, -0.5f, -0.5f) + offset, new Vector3(0, -0.5f, 0.5f) + offset, new Vector3(0, 0.5f, 0.5f) + offset };
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
				result.ProjectUV(cubeTextureCoordinates, GeneralDirection3D.Back);
			return result;
		}
		return cubeTextureCoordinates.GetCubeSide(GeneralDirection3D.Forward);
	}

	// ---------------------------------------------------------------------------------------------------------------

	protected override void SetupVoxelData(VoxelMap map, List<Vector3Int> voxelPositions, int shapeIndex)
	{
		for (int i = 0; i < voxelPositions.Count; i++)
			SetupVoxelTypeAndRotation(map, voxelPositions[i], shapeIndex);
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
				CubicTransformation transformation = GetTransformation(v.extraVoxelData);
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

	// ---------------------------------- Mesh Generation ------------------------------------------------------------

	protected sealed override void GenerateMeshData(
		VoxelMap map, List<Vector3Int> voxelPositions,
		int shapeIndex, MeshBuilder meshBuilder)
	{
		for (int i = 0; i < voxelPositions.Count; i++)
			BuildMesh(map, voxelPositions[i], meshBuilder);
	}


	readonly GeneralDirection3D[] stairNeighbourDirections = new GeneralDirection3D[4];
	readonly GeneralDirection3D[] wallNeighbourDirections = new GeneralDirection3D[3];

	void SetupVoxelTypeAndRotation(VoxelMap map, Vector3Int position, int shapeIndex)
	{
		Voxel voxelData = map.GetVoxel(position);
		ushort extraVoxelData = voxelData.extraVoxelData;
		bool useAutoSetup = GetAutoSetup(extraVoxelData);
		if (!useAutoSetup) return;

		// SetupFromMesh Rotation & StairShape
		int wallNeighbourCount = 0;
		int stairNeighbourCount = 0;

		foreach (GeneralDirection3D direction in DirectionUtility.generalDirection3DValues)
		{
			Vector3Int directionVector = direction.ToVectorInt();

			if (map.TryGetVoxel(position + directionVector, out Voxel neighbour))
			{
				if (neighbour.shapeId == shapeIndex)
				{
					stairNeighbourDirections[stairNeighbourCount] = direction;
					stairNeighbourCount++;
					if (stairNeighbourCount == 4)
						break;
				}
				else if (neighbour.IsFilled())
				{
					wallNeighbourDirections[stairNeighbourCount] = direction;
					wallNeighbourCount++;
					if (stairNeighbourCount == 3)
						break;
				}
			}
		}


		ShapeType stairType = GetStairType(extraVoxelData);
		CubicTransformation transformation = GetTransformation(extraVoxelData);

		if (wallNeighbourCount == 1)
		{
			GeneralDirection3D down = wallNeighbourDirections[0];
			GeneralDirection3D up = down.Opposite();
			transformation.upDirection = up;
			if (stairNeighbourCount == 2)
			{
				GeneralDirection3D s1 = stairNeighbourDirections[0];
				GeneralDirection3D s2 = stairNeighbourDirections[1];
				if (s1 == s2.Opposite())
				{
					stairType = ShapeType.SimpleStair;
					transformation = CubicTransformation.FromRightUp(s1, up);
				}
				else
				{
					stairType = ShapeType.OuterCornerStair;
					transformation = CubicTransformation.FromRightForward(s1, s2);
				}
			}
		}
		else if (wallNeighbourCount == 2)
		{
			stairType = ShapeType.SimpleStair;
			GeneralDirection3D down = wallNeighbourDirections[0];
			GeneralDirection3D back = wallNeighbourDirections[1];
			GeneralDirection3D up = down.Opposite();
			GeneralDirection3D forward = back.Opposite();
			if (up.GetAxis() != forward.GetAxis())
				transformation = CubicTransformation.FromUpForward(up, forward);
		}
		else if (wallNeighbourCount == 3)
		{
			stairType = ShapeType.InnerCornerStair;
			// TODO
			/*
			GeneralDirection3D down = wallNeighbourDirections[0];
			GeneralDirection3D back = wallNeighbourDirections[1];
			GeneralDirection3D left = wallNeighbourDirections[2];

			GeneralDirection3D up = down.Opposite();
			GeneralDirection3D forward = back.Opposite();
			GeneralDirection3D right = left.Opposite();
			if (stairNeighbourCount == 2)
			{
			}
			// transformation = CubicTransformation.FromRightUp(right, up);
			*/
		}
		else if (wallNeighbourCount >= 4)
		{
			stairType = ShapeType.FullBlock;
		}

		extraVoxelData = SetStairType(extraVoxelData, stairType);
		extraVoxelData = SetTransformation(extraVoxelData, transformation);
		voxelData.extraVoxelData = extraVoxelData;
		map.SetVoxel(position, voxelData);
	}

	void BuildMesh(VoxelMap map, Vector3Int position, MeshBuilder meshBuilder)
	{
		Voxel voxelData = map.GetVoxel(position);
		ushort extraVoxelData = voxelData.extraVoxelData;
		ShapeType stairType = GetStairType(extraVoxelData);

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
				// TODO: Build FullBlock ???
				break;
		}
	}

	void BuildSimpleStair(VoxelMap map, Vector3Int position, Voxel voxelData, MeshBuilder meshBuilder)
	{
		ushort extraVoxelData = voxelData.extraVoxelData;
		CubicTransformation transformation = GetTransformation(extraVoxelData);
		byte transformationIndex = transformation.GetIndex();

		bool autoSet = GetAutoSetup(extraVoxelData);
		Vector3 center = position + half;

		GeneralDirection3D back = transformation.TransformDirection(GeneralDirection3D.Forward);
		GeneralDirection3D forward = back.Opposite();
		GeneralDirection3D right = transformation.TransformDirection(GeneralDirection3D.Right);
		GeneralDirection3D left = right.Opposite();

		bool forwardFilled = map.IsFilledSafe(position + forward.ToVectorInt(), forward);
		bool upFilled = map.IsFilledSafe(position + Vector3Int.up, GeneralDirection3D.Up);

		bool downFilled = map.IsFilledSafe(position + Vector3Int.down, GeneralDirection3D.Down);
		bool backFilled = map.IsFilledSafe(position + back.ToVectorInt(), forward);
		if (!downFilled)
			meshBuilder.Add(transformedBottomSide[transformationIndex], center);

		if (!backFilled)
			meshBuilder.Add(transformedBackSide[transformationIndex], center);

		bool addStair = !forwardFilled || !upFilled;

		if (addStair)
		{
			meshBuilder.Add(transformedStairs[transformationIndex], center);
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
				meshBuilder.Add(transformedRightSide[transformationIndex], center);
			else
			{
				CubicTransformation t = transformation;
				t.verticalRotation += 1;
				meshBuilder.Add(transformedBackSide[t.GetIndex()], center);
			}
		}

		if (closeLeft)
		{
			if (addStair)
				meshBuilder.Add(transformedLeftSide[transformationIndex], center);
			else
			{
				CubicTransformation t = transformation;
				t.verticalRotation += 3;
				meshBuilder.Add(transformedBackSide[t.GetIndex()], center);
			}
		}
	}

	void BuildCorner(VoxelMap map, Vector3Int position, Voxel voxelData, bool isInner, MeshBuilder meshBuilder)
	{
		ushort extraVoxelData = voxelData.extraVoxelData;
		CubicTransformation transformation = GetTransformation(extraVoxelData);
		byte transformationIndex = transformation.GetIndex();
		bool autoSet = GetAutoSetup(extraVoxelData);
		// int level = extraVoxelData.Get2Bit(extraInfo_level);
		Vector3 center = position + half;

		// TOP MESH 
		if (isInner)
			meshBuilder.Add(transformedInnerCorners[transformationIndex], center);
		else
			meshBuilder.Add(transformedOuterCorner[transformationIndex], center);

		// BOTTOM MESH

		bool downFilled = map.IsFilledSafe(position + Vector3Int.down, GeneralDirection3D.Down);
		if (!downFilled)
			meshBuilder.Add(transformedBottomSide[transformationIndex], center);

		// SIDE MESHES

		Vector3Int stairUpward = GetCornerDirection(transformation);
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
			{
				CubicTransformation t = transformation;
				t.verticalRotation += 3;
				meshBuilder.Add(transformedLeftSide[t.GetIndex()], center);
			}
			if (closeRightSide)
				meshBuilder.Add(transformedRightSide[transformationIndex], center);
		}
		else
		{
			if (closeLeftSide)
				meshBuilder.Add(transformedLeftSide[transformationIndex], center);
			if (closeRightSide)
			{
				CubicTransformation t = transformation;
				t.verticalRotation += 3;
				meshBuilder.Add(transformedRightSide[t.GetIndex()], center);
			}
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
				meshBuilder.Add(transformedBackSide[transformationIndex], center);
			if (!isBack2Filled)
			{

				CubicTransformation t = transformation;
				t.verticalRotation += 3;
				meshBuilder.Add(transformedBackSide[t.GetIndex()], center);
			}
		}
	}

	// ROTATION HELPER METHODSS ------------

	//int GetTransformation(int initialRotation, Vector3Int upDirection)
	//{
	//	int upX = upDirection.x;
	//	int upZ = upDirection.z;

	//	if (upX == 0 && upZ == 0) return initialRotation;

	//	if (upX == 0 || upZ == 0)  // SimpleStair
	//	{
	//		if (upZ == 1) return 0; // Forward
	//		if (upX == 1) return 1; // Right
	//		if (upZ == -1) return 2; // Back
	//		if (upX == -1) return 3; // Left
	//	}

	//	if (upZ == 1)  // Corner
	//	{
	//		if (upX == 1) return 1; // Forward - Right
	//		if (upX == -1) return 0; // Forward - Left
	//	}
	//	else
	//	{
	//		if (upX == 1) return 2; // Back - Right
	//		if (upX == -1) return 3; // Left - Left
	//	}

	//	return initialRotation;
	//}


	Vector3Int GetCornerDirection(CubicTransformation rotation)
		=> rotation.TransformDirection(GeneralDirection3D.Forward).ToVectorInt() + rotation.TransformDirection(GeneralDirection3D.Left).ToVectorInt();

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
				name = "Auto Setup",
				getValue = GetAutoSetup,
				setValue = SetAutoSetup
			},
			new ExtraVoxelControl<ShapeType>()
			{
				name = "Stair Type",
				getValue = GetStairType,
				setValue = SetStairType
			},
			new ExtraVoxelControl<GeneralDirection3D> ()
			{
				name = "Up Direction",
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
			new ExtraVoxelControl<int>
			{
				name = "Level",
				getValue = GetLevel,
				setValue = SetLevel
			}
		};
		return controls;
	}


	const int extraInfo_isAutoSet = 4;
	const int extraInfo_stairType = 5;
	const int extraInfo_level = 6;

	static bool GetAutoSetup(ushort extraVoxelData) => extraVoxelData.Get2Bit(extraInfo_isAutoSet) == 0;
	static ushort SetAutoSetup(ushort originalExtraVoxelData, bool newValue)
	{
		ushort newData = originalExtraVoxelData.Set2Bit(extraInfo_isAutoSet, newValue ? 0 : 1);
		return newData;
	}

	static ShapeType GetStairType(ushort voxelData) => (ShapeType)voxelData.Get2Bit(extraInfo_stairType);
	static ushort SetStairType(ushort originalExtraVoxelData, ShapeType newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_stairType, (int)newValue);
	static int GetLevel(ushort extraVoxelData) => extraVoxelData.Get2Bit(extraInfo_level);
	static ushort SetLevel(ushort originalExtraVoxelData, int newValue) =>
		originalExtraVoxelData.Set2Bit(extraInfo_level, newValue);

	static CubicTransformation GetTransformation(ushort extraVoxelData) =>
		new(extraVoxelData.GetByte(0));
	static ushort SetTransformation(ushort originalExtraVoxelData, CubicTransformation newValue) =>
		originalExtraVoxelData.SetByte(0, newValue.GetIndex());

	static GeneralDirection3D GetUpDirection(ushort extraVoxelData) => GetTransformation(extraVoxelData).upDirection;

	static ushort SetUpDirection(ushort originalExtraValue, GeneralDirection3D value)
	{
		CubicTransformation cubicTransformation = new((byte)originalExtraValue)
		{
			upDirection = value
		};
		return originalExtraValue.SetByte(0, cubicTransformation.GetIndex());
	}
	static int GetRotation(ushort extraVoxelData) => GetTransformation(extraVoxelData).verticalRotation;
	static ushort SetRotation(ushort originalExtraValue, int value)
	{
		value %= 4;
		CubicTransformation cubicTransformation = new((byte)originalExtraValue)
		{
			verticalRotation = value
		};
		return originalExtraValue.SetByte(0, cubicTransformation.GetIndex());
	}

	static bool GetFlip(ushort extraVoxelData) => GetTransformation(extraVoxelData).verticalFlip;

	static ushort SetFlip(ushort originalExtraValue, bool value)
	{
		CubicTransformation cubicTransformation = new((byte)originalExtraValue)
		{
			verticalFlip = value
		};
		return originalExtraValue.SetByte(0, cubicTransformation.GetIndex());
	}


	protected override PhysicalVoxelShape PhysicalShape(ushort extraData)   // REMOVE
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

	GeneralDirection3D GetStairRightSideGlobal(ushort extraData) => GetStairType(extraData) switch
	{
		ShapeType.OuterCornerStair => GetTransformation(extraData).TransformDirection(GeneralDirection3D.Forward),
		_ => GetTransformation(extraData).TransformDirection(GeneralDirection3D.Right),
	};

	GeneralDirection3D GetStairLeftSideGlobal(ushort extraData) => GetStairType(extraData) switch
	{
		ShapeType.InnerCornerStair => GetTransformation(extraData).TransformDirection(GeneralDirection3D.Back),
		_ => GetTransformation(extraData).TransformDirection(GeneralDirection3D.Left),
	};

	public override void BuildPhysicalMeshSides(FlexibleMesh flexMesh, VoxelMap map, Vector3Int voxelPoint, ref int sideCounter)
	{
		// GenerateRotatedPhysical();

		Voxel voxel = map.GetVoxel(voxelPoint);
		CubicTransformation transformation = GetTransformation(voxel.extraVoxelData);
		ShapeType stairType = GetStairType(voxel.extraVoxelData);

		GeneralDirection3D localBackInGlobal = transformation.TransformDirection(GeneralDirection3D.Back);


		if (stairType == ShapeType.SimpleStair)
		{
			flexMesh.AddFace(ramp, voxelPoint);

			// if(voxel.IsSideClosed(localBackInGlobal))
			flexMesh.AddFace(forwardFull, voxelPoint);

			if (voxel.IsSideClosed(GeneralDirection3D.Down))
				flexMesh.AddFace(bottomFull, voxelPoint);
			return;
		}
	}
}