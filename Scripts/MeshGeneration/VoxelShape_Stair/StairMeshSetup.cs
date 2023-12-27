using MeshUtility;
using MUtility;
using System;
using UnityEngine;

namespace VoxelSystem
{

	[Serializable]
	class StairMeshSetup
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
		// Back2 -> right
		// Back2 -> forward
		// RightSide -> left
		// LeftSide -> back

		// OUTER CORNER
		// RightSide -> right
		// LeftSide -> forward


		// Setup Data ------------------------------------------------------------------------

		[SerializeField, Tooltip("The stair goes down looking FORWARD\n")]
		public Mesh simpleStairs;
		[SerializeField, Tooltip("The corners of the stair looking LEFT-FORWARD\n")]
		public Mesh innerCornerStairs;
		[SerializeField, Tooltip("The corners of the stair looking LEFT-FORWARD\n")]
		public Mesh outerCornerStairs;
		[SerializeField, Tooltip("The side of the stair that goes down looking FORWARD")]
		public Mesh rightSide;
		[SerializeField, Tooltip("The side of the stair that goes down looking FORWARD")]
		public Mesh leftSide;
		[SerializeField, Tooltip("The back of the Mesh looking FORWARD")]
		public Mesh backSide;
		[SerializeField, Tooltip("The back of the Mesh looking Down Globally")]
		public Mesh bottom;
		[Space]
		[SerializeField] bool autoConvertFromRightHanded = true;

		// Constant Data ------------------------------------------------------------------------

		const int transformCount = CubicTransformation.allTransformationCount;

		static readonly MeshBuilder simpleStairs_Default = new(
			new Vector3[] { new(0, 0, 0), new(0, 1, 1), new(1, 1, 1), new(1, 0, 0) },
			new Vector3[] { new(0, 1, -1), new(0, 1, -1), new(0, 1, -1), new(0, 1, -1), new(0, 1, -1), new(0, 1, -1) },
			new Vector2[] { new(0, 0), new(0, 1), new(1, 1), new(1, 0) },
			new int[] { 0, 1, 2, 0, 2, 3 }
		);

		static readonly MeshBuilder outerCornerStairs_Default = new(
			new Vector3[] {
				new(0, 0, 0), new(1, 1, 1), new(1, 0, 0),  
				new(0, 0, 0), new(0, 0, 1), new(1, 1, 1),},
			new Vector3[] { new(0, 1, -1), new(0, 1, -1), new(0, 1, -1), new(-1, 1, 0), new(-1, 1, 0), new(-1, 1, 0) },
			new Vector2[] { new(1, 0), new(0, 1), new(1, 1), new(1, 0), new(0, 0), new(0, 1) },
			new int[] { 0, 1, 2, 3, 4, 5 }
		);

		static readonly MeshBuilder innerCornerStairs_Default = new(
			new Vector3[] {
				new(0, 0, 0), new(1, 1, 1), new(1, 1, 0),
				new(0, 0, 0), new(0, 1, 1), new(1, 1, 1), },
			new Vector3[] { new(-1, 1, 0), new(-1, 1, 0), new(-1, 1, 0), new(0, 1, -1), new(0, 1, -1), new(0, 1, -1) },
			new Vector2[] { new(1, 0), new(0, 1), new(1, 1), new(1, 0), new(0, 0), new(0, 1) },
			new int[] { 0, 1, 2, 3, 4, 5 }
		);

		static readonly MeshBuilder bottom_Default = new(
			new Vector3[] { new(0, 0, 0), new(1, 0, 0), new(1, 0, 1), new(0, 0, 1) },
			new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back },
			new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) },
			new int[] { 0, 1, 2, 0, 2, 3 }
		);

		static readonly MeshBuilder backSide_Default = new(
			new Vector3[] { new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1) },
			new Vector3[] { new(0, 0, 1), new(0, 0, 1), new(0, 0, 1), new(0, 0, 1), new(0, 0, 1), new(0, 0, 1) },
			new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) },
			new int[] { 0, 1, 2, 0, 2, 3 }
		);

		static readonly MeshBuilder leftSide_Default = new(
			new Vector3[] { new(0, 0, 0), new(0, 0, 1), new(0, 1, 1) },
			new Vector3[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left },
			new Vector2[] { new(0, 0), new(1, 0), new(1, 1) },
			new int[] { 0, 1, 2 }
		);
		static readonly MeshBuilder rightSide_Default = new(
			new Vector3[] { new(1, 0, 0), new(1, 0, 1), new(1, 1, 1) },
			new Vector3[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right },
			new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(1, 1) },
			new int[] { 2, 1, 0 }
		);

		// Cached Data ------------------------------------------------------------------------

		[SerializeField, HideInInspector] public MeshBuilder[] transformedSimpleStairs;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedInnerCornerStairs;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedOuterCornerStairs;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedRightSide;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedRightSide2;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedLeftSide;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedLeftSide2;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedBackSide;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedBackSide2;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedBottom;

		[SerializeField, HideInInspector] public MeshBuilder[] transformedSimpleStairs_Default;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedInnerCornerStairs_Default;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedOuterCornerStairs_Default;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedRightSide_Default;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedRightSide2_Default;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedLeftSide_Default;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedLeftSide2_Default;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedBackSide_Default;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedBackSide2_Default;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedBottom_Default;

		// Initialization ----------------------------------------------------------------------------

		public bool IsInitialized => transformedSimpleStairs != null;

		public void Initialize(CubeUVSetup uvSetup, bool remapUV)
		{
			transformedSimpleStairs = Setup(transformedSimpleStairs);
			transformedInnerCornerStairs = Setup(transformedInnerCornerStairs);
			transformedOuterCornerStairs = Setup(transformedOuterCornerStairs);
			transformedRightSide = Setup(transformedRightSide);
			transformedLeftSide = Setup(transformedLeftSide);
			transformedBackSide = Setup(transformedBackSide);
			transformedBottom = Setup(transformedBottom);

			transformedRightSide2 = Setup(transformedRightSide2);
			transformedLeftSide2 = Setup(transformedLeftSide2);
			transformedBackSide2 = Setup(transformedBackSide2);

			static MeshBuilder[] Setup(MeshBuilder[] array)
			{
				if (array == null || array.Length != transformCount)
				{
					array = new MeshBuilder[transformCount];
					for (int i = 0; i < transformCount; i++)
					{
						array[i] = new MeshBuilder();
					}
				}
				return array;
			}

			if (!remapUV)
				uvSetup = null;

			Matrix4x4 rotateRight = Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0));

			for (byte i = 0; i < CubicTransformation.allTransformationCount; i++)
			{
				CubicTransformation ct = new(i);
				Matrix4x4 transformation = ct.GetTransformationMatrix();
				transformation = Matrix4x4.Translate(Vector3.one * 0.5f) * transformation;

				transformedSimpleStairs[i] = GenerateSide(simpleStairs_Default, simpleStairs, uvSetup, GeneralDirection3D.Up);
				transformedSimpleStairs[i].Transform(transformation);
				transformedInnerCornerStairs[i] = GenerateSide(innerCornerStairs_Default, innerCornerStairs, uvSetup, GeneralDirection3D.Up);
				transformedInnerCornerStairs[i].Transform(transformation);
				transformedOuterCornerStairs[i] = GenerateSide(outerCornerStairs_Default, outerCornerStairs, uvSetup, GeneralDirection3D.Up);
				transformedOuterCornerStairs[i].Transform(transformation);
				transformedRightSide[i] = GenerateSide(rightSide_Default, rightSide, uvSetup, GeneralDirection3D.Right);
				transformedRightSide[i].Transform(transformation);
				transformedLeftSide[i] = GenerateSide(leftSide_Default, leftSide, uvSetup, GeneralDirection3D.Left);
				transformedLeftSide[i].Transform(transformation);
				transformedBackSide[i] = GenerateSide(backSide_Default, backSide, uvSetup, GeneralDirection3D.Forward);
				transformedBackSide[i].Transform(transformation);
				transformedBottom[i] = GenerateSide(bottom_Default, bottom, uvSetup, GeneralDirection3D.Down);
				transformedBottom[i].Transform(transformation);

				transformedRightSide2[i] = GenerateSide(rightSide_Default, rightSide, uvSetup, GeneralDirection3D.Right);
				transformedRightSide2[i].Transform(rotateRight); 
				transformedRightSide2[i].Transform(transformation);
				transformedLeftSide2[i] = GenerateSide(leftSide_Default, leftSide, uvSetup, GeneralDirection3D.Left);
				transformedLeftSide2[i].Transform(rotateRight);
				transformedLeftSide2[i].Transform(transformation);
				transformedBackSide2[i] = GenerateSide(backSide_Default, backSide, uvSetup, GeneralDirection3D.Forward);
				transformedBackSide2[i].Transform(rotateRight);
				transformedBackSide2[i].Transform(transformation);
			}
		}
		MeshBuilder GenerateSide(MeshBuilder defaultVersion, Mesh setupVersion, CubeUVSetup uvSetup, GeneralDirection3D uvProjection)
		{
			MeshBuilder builder;
			if (setupVersion == null)
			{
				builder = defaultVersion.GetCopy();
				builder.Translate(Vector3.one * -0.5f);
			}
			else
			{
				builder = new(setupVersion, autoConvertFromRightHanded);
			}

			if (uvSetup != null)
				builder.ProjectUV(uvSetup, uvProjection);

			return builder;
		}

		// ------- Mesh Generation ---------------------------------------------------------------------------


		public void BuildMesh(
			Vector3Int position, int shapeID, StairShape stairType,
			CubicTransformation transformation, bool autoSet,
			VoxelMap map, MeshBuilder meshBuilder)
		{
			switch (stairType)
			{
				case StairShape.SimpleStair:
					BuildSimpleStair(map, position, shapeID, transformation, autoSet, meshBuilder);
					break;
				case StairShape.InnerCornerStair:
					BuildInnerCorner(map, position, shapeID, transformation, autoSet, meshBuilder);
					break;
				case StairShape.OuterCornerStair:
					BuildOuterCorner(map, position, shapeID, transformation, autoSet, meshBuilder);
					break;
				case StairShape.FullBlock:
					// TODO: Build FullBlock ???
					break;
			}
		}

		void BuildSimpleStair(VoxelMap map, Vector3Int position, int shapeID, CubicTransformation transformation, bool autoSet, MeshBuilder meshBuilder)
		{
			byte transformationIndex = transformation.GetIndex();

			// STAIR MESH 
			meshBuilder.Add(transformedSimpleStairs[transformationIndex], position);

			// BOTTOM MESH
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedBottom, GeneralDirection3D.Down, meshBuilder);

			// BACK MESH 
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedBackSide, GeneralDirection3D.Forward, meshBuilder);

			// SIDE MESHES
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedRightSide, GeneralDirection3D.Right, meshBuilder);
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedLeftSide, GeneralDirection3D.Left, meshBuilder);
		}

		void BuildOuterCorner(VoxelMap map, Vector3Int position, int shapeID, CubicTransformation transformation, bool autoSet, MeshBuilder meshBuilder)
		{
			byte transformationIndex = transformation.GetIndex();

			// STAIR MESH 
			meshBuilder.Add(transformedOuterCornerStairs[transformationIndex], position);

			// BOTTOM MESH
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedBottom, GeneralDirection3D.Down, meshBuilder);

			// SIDE MESHES 
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedLeftSide2, GeneralDirection3D.Forward, meshBuilder);
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedRightSide, GeneralDirection3D.Right, meshBuilder);
		}

		void BuildInnerCorner(VoxelMap map, Vector3Int position, int shapeID, CubicTransformation transformation, bool autoSet, MeshBuilder meshBuilder)
		{
			byte transformationIndex = transformation.GetIndex();

			// STAIR MESH 
			meshBuilder.Add(transformedInnerCornerStairs[transformationIndex], position);

			// BOTTOM MESH
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedBottom, GeneralDirection3D.Down, meshBuilder);

			// BACK MESHES
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedBackSide, GeneralDirection3D.Forward, meshBuilder);
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedBackSide2, GeneralDirection3D.Right, meshBuilder);

			// SIDE MESHES
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedLeftSide, GeneralDirection3D.Left, meshBuilder);
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedRightSide2, GeneralDirection3D.Back, meshBuilder);
		}

		void AddMeshIfSideOpen(VoxelMap map, Vector3Int position, CubicTransformation transformation, byte transformationIndex, MeshBuilder[] transformed, GeneralDirection3D direction, MeshBuilder meshBuilder)
		{
			GeneralDirection3D transformedDir = transformation.TransformDirection(direction);
			bool isSideFilled = map.IsFilledSafe(position + transformedDir.ToVectorInt(), transformedDir.Opposite());
			if (!isSideFilled)
				meshBuilder.Add(transformed[transformationIndex], position);
		}

		void AddSideMesh(VoxelMap map, Vector3Int position, int shapeID, CubicTransformation transformation, byte transformationIndex, bool autoSet, MeshBuilder[] transformed, GeneralDirection3D direction, MeshBuilder meshBuilder)
		{
			GeneralDirection3D transformedDir = transformation.TransformDirection(direction);
			bool closeSide = true;
			if (map.TryGetVoxel(position + transformedDir.ToVectorInt(), out Voxel neighbor))
			{
				if (autoSet && neighbor.shapeId == shapeID)
					closeSide = false;
				else
					closeSide = !neighbor.IsFilled(transformedDir.Opposite());
			}
			if (closeSide)
				meshBuilder.Add(transformed[transformationIndex], position);
		}
	}
}
