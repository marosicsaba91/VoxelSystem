using VoxelSystem.MeshUtility;
using MUtility;
using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace VoxelSystem
{

	[Serializable]
	[NoAutoStaticsCleanup]
	class StairMeshSetup
	{
		// SIMPLE STAIR ROTATIONS
		// transformation index == 0 -> stair rises in forward diagonal
		// transformation index == 1 -> stair rises in leftSideVector diagonal
		// transformation index == 2 -> stair rises in back diagonal
		// transformation index == 3 -> stair rises in rightSideVector diagonal

		// CORNER STAIR ROTATIONS
		// transformation index == 0 -> stair rises in forward-rightSideVector diagonal
		// transformation index == 1 -> stair rises in forward-leftSideVector diagonal
		// transformation index == 2 -> stair rises in back-leftSideVector diagonal
		// transformation index == 3 -> stair rises in back-rightSideVector diagonal

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
		[SerializeField, Tooltip("The front of the Mesh looking FORWARD")]
		[FormerlySerializedAs("backSide")] public Mesh frontSide;
		[SerializeField, Tooltip("The bottom of the Mesh looking Down Globally")]
		public Mesh bottom;
		[Space]
		[SerializeField] bool autoConvertFromRightHanded = true;

		// Constant Data ------------------------------------------------------------------------

		const int transformCount = CubicTransformation.allTransformationCount;

		static readonly Vector3[] _simpleStairs_Points = { new(0, 0, 0), new(0, 1, 1), new(1, 1, 1), new(1, 0, 0) };
		static readonly Vector3[] _outerCornerStairs_Points1 = { new(0, 0, 0), new(1, 1, 1), new(1, 0, 0) };
		static readonly Vector3[] _outerCornerStairs_Points2 = { new(0, 0, 0), new(0, 0, 1), new(1, 1, 1) };
		static readonly Vector3[] _innerCornerStairs_Points1 = { new(0, 0, 0), new(1, 1, 1), new(1, 1, 0) };
		static readonly Vector3[] _innerCornerStairs_Points2 = { new(0, 0, 0), new(0, 1, 1), new(1, 1, 1) };
		static readonly Vector3[] _bottom_Points = { new(0, 0, 0), new(1, 0, 0), new(1, 0, 1), new(0, 0, 1) };
		static readonly Vector3[] _leftSide_Points = { new(0, 0, 0), new(0, 0, 1), new(0, 1, 1) };
		static readonly Vector3[] _rightSide_Points = { new(1, 0, 1), new(1, 0, 0), new(1, 1, 1) };
		static readonly Vector3[] _frontSide_Points = { new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1) };

		static readonly Vector3[] _leftSide_Points2 = { new(0, 0, 1), new(1, 0, 1), new(1, 1, 1) };
		static readonly Vector3[] _rightSide_Points2 = { new(1, 0, 0), new(0, 0, 0), new(1, 1, 0) };
		static readonly Vector3[] _frontSide_Points2 = { new(1, 0, 0), new(1, 1, 0), new(1, 1, 1), new(1, 0, 1) };

		public static readonly Vector3[][] simpleStair_PhysicalSides = { _simpleStairs_Points, _bottom_Points, _frontSide_Points, _rightSide_Points, _leftSide_Points };
		public static readonly Vector3[][] innerCorner_PhysicalSides = { _innerCornerStairs_Points1, _innerCornerStairs_Points2, _bottom_Points, _frontSide_Points, _frontSide_Points2, _rightSide_Points2, _leftSide_Points };
		public static readonly Vector3[][] outerCorner_PhysicalSides = { _outerCornerStairs_Points1, _outerCornerStairs_Points2, _bottom_Points, _rightSide_Points, _leftSide_Points2 };

		static readonly MeshBuilder _simpleStairs_Default = new(
			_simpleStairs_Points,
			new Vector3[] { new(0, 1, -1), new(0, 1, -1), new(0, 1, -1), new(0, 1, -1), new(0, 1, -1), new(0, 1, -1) },
			new Vector2[] { new(0, 0), new(0, 1), new(1, 1), new(1, 0) },
			new [] { 0, 1, 2, 0, 2, 3 }
		);

		static readonly MeshBuilder _outerCornerStairs_Default = new(
			new Vector3[] {
				new(0, 0, 0), new(1, 1, 1), new(1, 0, 0),
				new(0, 0, 0), new(0, 0, 1), new(1, 1, 1),},
			new Vector3[] { new(0, 1, -1), new(0, 1, -1), new(0, 1, -1), new(-1, 1, 0), new(-1, 1, 0), new(-1, 1, 0) },
			new Vector2[] { new(1, 0), new(0, 1), new(1, 1), new(1, 0), new(0, 0), new(0, 1) },
			new [] { 0, 1, 2, 3, 4, 5 }
		);

		static readonly MeshBuilder _innerCornerStairs_Default = new(
			new Vector3[] {
				new(0, 0, 0), new(1, 1, 1), new(1, 1, 0),
				new(0, 0, 0), new(0, 1, 1), new(1, 1, 1), },
			new Vector3[] { new(-1, 1, 0), new(-1, 1, 0), new(-1, 1, 0), new(0, 1, -1), new(0, 1, -1), new(0, 1, -1) },
			new Vector2[] { new(1, 0), new(0, 1), new(1, 1), new(1, 0), new(0, 0), new(0, 1) },
			new [] { 0, 1, 2, 3, 4, 5 }
		);

		static readonly MeshBuilder _bottom_Default = new(
			_bottom_Points,
			new [] { Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back },
			new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) },
			new [] { 0, 1, 2, 0, 2, 3 }
		);

		static readonly MeshBuilder _frontSide_Default = new(
			_frontSide_Points,
			new Vector3[] { new(0, 0, 1), new(0, 0, 1), new(0, 0, 1), new(0, 0, 1), new(0, 0, 1), new(0, 0, 1) },
			new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) },
			new [] { 0, 1, 2, 0, 2, 3 }
		);

		static readonly MeshBuilder _leftSide_Default = new(
			_leftSide_Points,
			new [] { Vector3.left, Vector3.left, Vector3.left, Vector3.left },
			new Vector2[] { new(0, 0), new(1, 0), new(1, 1) },
			new [] { 0, 1, 2 }
		);
		static readonly MeshBuilder _rightSide_Default = new(
			_rightSide_Points,
			new [] { Vector3.right, Vector3.right, Vector3.right, Vector3.right },
			new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(1, 1) },
			new [] { 0, 1, 2 }
		);



		// Cached Data ------------------------------------------------------------------------

		[SerializeField, HideInInspector] public MeshBuilder[] transformedSimpleStairs;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedInnerCornerStairs;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedOuterCornerStairs;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedRightSide;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedRightSide2;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedLeftSide;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedLeftSide2;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedFrontSide;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedFrontSide2;
		[SerializeField, HideInInspector] public MeshBuilder[] transformedBottom;

		// Initialization ----------------------------------------------------------------------------

		public bool IsInitialized => transformedSimpleStairs != null;

		public void Initialize(CubeUVSetup uvSetup, bool remapUV)
		{
			transformedSimpleStairs = Setup(transformedSimpleStairs);
			transformedInnerCornerStairs = Setup(transformedInnerCornerStairs);
			transformedOuterCornerStairs = Setup(transformedOuterCornerStairs);
			transformedRightSide = Setup(transformedRightSide);
			transformedLeftSide = Setup(transformedLeftSide);
			transformedFrontSide = Setup(transformedFrontSide);
			transformedBottom = Setup(transformedBottom);

			transformedRightSide2 = Setup(transformedRightSide2);
			transformedLeftSide2 = Setup(transformedLeftSide2);
			transformedFrontSide2 = Setup(transformedFrontSide2); 
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

				transformedSimpleStairs[i] = GenerateSide(_simpleStairs_Default, simpleStairs, transformation, uvSetup, GeneralDirection3D.Up); 
				transformedInnerCornerStairs[i] = GenerateSide(_innerCornerStairs_Default, innerCornerStairs, transformation, uvSetup, GeneralDirection3D.Up); 
				transformedOuterCornerStairs[i] = GenerateSide(_outerCornerStairs_Default, outerCornerStairs, transformation, uvSetup, GeneralDirection3D.Up); 
				transformedRightSide[i] = GenerateSide(_rightSide_Default, rightSide, transformation, uvSetup, GeneralDirection3D.Right); 
				transformedLeftSide[i] = GenerateSide(_leftSide_Default, leftSide, transformation, uvSetup, GeneralDirection3D.Left); 
				transformedFrontSide[i] = GenerateSide(_frontSide_Default, frontSide, transformation, uvSetup, GeneralDirection3D.Forward); 
				transformedBottom[i] = GenerateSide(_bottom_Default, bottom, transformation, uvSetup, GeneralDirection3D.Down); 

				transformedRightSide2[i] = GenerateSide(_rightSide_Default, rightSide, transformation * rotateRight, uvSetup, GeneralDirection3D.Right); 
				transformedLeftSide2[i] = GenerateSide(_leftSide_Default, leftSide, transformation * rotateRight, uvSetup, GeneralDirection3D.Left); 
				transformedFrontSide2[i] = GenerateSide(_frontSide_Default, frontSide, transformation * rotateRight, uvSetup, GeneralDirection3D.Forward);
			}
		}

		MeshBuilder GenerateSide(MeshBuilder defaultVersion, Mesh setupVersion, Matrix4x4 transformation, CubeUVSetup uvSetup, GeneralDirection3D uvProjection)
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

			builder.Transform(transformation);

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
			byte transformationIndex = transformation.ToByte();

			// STAIR MESH 
			meshBuilder.Add(transformedSimpleStairs[transformationIndex], position);

			// BOTTOM MESH
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedBottom, GeneralDirection3D.Down, meshBuilder);

			// BACK MESH 
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedFrontSide, GeneralDirection3D.Forward, meshBuilder);

			// SIDE MESHES
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedRightSide, GeneralDirection3D.Right, meshBuilder);
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedLeftSide, GeneralDirection3D.Left, meshBuilder);
		}

		void BuildOuterCorner(VoxelMap map, Vector3Int position, int shapeID, CubicTransformation transformation, bool autoSet, MeshBuilder meshBuilder)
		{
			byte transformationIndex = transformation.ToByte();

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
			byte transformationIndex = transformation.ToByte();

			// STAIR MESH 
			meshBuilder.Add(transformedInnerCornerStairs[transformationIndex], position);

			// BOTTOM MESH
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedBottom, GeneralDirection3D.Down, meshBuilder);

			// BACK MESHES
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedFrontSide, GeneralDirection3D.Forward, meshBuilder);
			AddMeshIfSideOpen(map, position, transformation, transformationIndex, transformedFrontSide2, GeneralDirection3D.Right, meshBuilder);

			// SIDE MESHES
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedLeftSide, GeneralDirection3D.Left, meshBuilder);
			AddSideMesh(map, position, shapeID, transformation, transformationIndex, autoSet, transformedRightSide2, GeneralDirection3D.Back, meshBuilder);
		}

		void AddMeshIfSideOpen(VoxelMap map, Vector3Int position, CubicTransformation transformation, byte transformationIndex, MeshBuilder[] transformed, GeneralDirection3D localDirection, MeshBuilder meshBuilder)
		{
			GeneralDirection3D transformedDir = transformation.TransformDirection(localDirection);
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
