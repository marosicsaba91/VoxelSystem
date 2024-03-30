using EasyEditor;
using MeshUtility;
using Microsoft.SqlServer.Server;
using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public enum StairShape
	{
		SimpleStair = 0,
		InnerCornerStair = 1,
		OuterCornerStair = 2,
		FullBlock = 3,
	}

	[CreateAssetMenu(fileName = "StairVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: Stair", order = EditorConstants.soOrder_VoxelShape)]
	public class VoxelShape_Stair : VoxelShapeBuilder
	{
		[Header("Mesh")]
		[SerializeField] StairMeshSetup meshSetup = new();

		[Header("Texture")]
		[SerializeField] bool overrideTextureCoordinates = true;
		[SerializeField, ShowIf(nameof(overrideTextureCoordinates))] CubeUVSetup cubeTextureCoordinates;

		[Header("Other Setup")]
		// [SerializeField, Range(1, 4)] int slope = 1;
		[SerializeField] bool isTransparent = false;

		public sealed override bool SupportsTransformation => true;

		protected sealed override bool IsInitialized => meshSetup.IsInitialized;
		protected override void InitializeCachedData() =>
			meshSetup.Initialize(cubeTextureCoordinates, overrideTextureCoordinates);

		// ---------------------------------------------------------------------------------------------------------------

		protected override void SetupVoxelData(VoxelMap map, List<Vector3Int> voxelPositions, int shapeIndex)
		{
			for (int i = 0; i < voxelPositions.Count; i++)
				SetupVoxelTypeAndTransformation(map, voxelPositions[i], shapeIndex);
		}

		readonly GeneralDirection3D[] stairNeighborsDirections = new GeneralDirection3D[6];
		readonly GeneralDirection3D[] wallNeighborsDirections = new GeneralDirection3D[6];
		void SetupVoxelTypeAndTransformation(VoxelMap map, Vector3Int position, int shapeId)
		{
			Voxel voxel = map.GetVoxel(position);
			byte extraVoxelData = voxel.extraData;
			bool useAutoSetup = GetAutoSetup(extraVoxelData);
			if (!useAutoSetup) return;

			// SetupFromMesh Rotation & StairShape
			int wallNeighborsCount = 0;
			int stairNeighborsCount = 0;
			foreach (GeneralDirection3D direction in DirectionUtility.generalDirection3DValues)
			{
				Vector3Int directionVector = direction.ToVectorInt();
				if (!map.TryGetVoxel(position + directionVector, out Voxel neighbor)) continue;
				if (!neighbor.IsFilled()) continue;

				if (neighbor.shapeId == shapeId)
				{
					stairNeighborsDirections[stairNeighborsCount] = direction;
					stairNeighborsCount++;
				}
				else
				{
					wallNeighborsDirections[wallNeighborsCount] = direction;
					wallNeighborsCount++;
				}
			}

			StairShape stairType = GetStairShape(extraVoxelData);
			CubicTransformation transformation = voxel.CubicTransformation;

			if (wallNeighborsCount == 1)
			{
				GeneralDirection3D down = wallNeighborsDirections[0];
				GeneralDirection3D up = down.Opposite();
				transformation.upDirection = up;
				if (stairNeighborsCount == 2)
				{
					GeneralDirection3D s1 = stairNeighborsDirections[0];
					GeneralDirection3D s2 = stairNeighborsDirections[1];
					if (s1 != s2.Opposite() && s1 != up && s2 != up)
					{
						stairType = StairShape.OuterCornerStair;

						if (DirectionUtility.IsLeftHanded(s1, up, s2))
							transformation = CubicTransformation.FromDirections(s1, up, s2);
						else
							transformation = CubicTransformation.FromDirections(s2, up, s1);
					}
				}

			}
			else if (wallNeighborsCount == 2)
			{
				stairType = StairShape.SimpleStair;
				GeneralDirection3D down = wallNeighborsDirections[0];
				GeneralDirection3D forward = wallNeighborsDirections[1];
				if (forward.GetAxis() == Axis3D.Y)
					(forward, down) = (down, forward);

				if (down.GetAxis() != forward.GetAxis())
					transformation = CubicTransformation.FromUpForward(down.Opposite(), forward);
			}
			else if (wallNeighborsCount == 3)
			{
				Axis3D a0 = wallNeighborsDirections[0].GetAxis();
				Axis3D a1 = wallNeighborsDirections[1].GetAxis();
				Axis3D a2 = wallNeighborsDirections[2].GetAxis();
				if (a0 != a1 && a0 != a2 && a1 != a2)
				{
					stairType = StairShape.InnerCornerStair;
					DirectionUtility.SortDirectionsByAxis(
						wallNeighborsDirections[0], wallNeighborsDirections[1], wallNeighborsDirections[2],
						out GeneralDirection3D xWall, out GeneralDirection3D yWall, out GeneralDirection3D zWall);

					if (stairNeighborsCount == 2)
					{
						GeneralDirection3D side1 = stairNeighborsDirections[0];
						GeneralDirection3D side2 = stairNeighborsDirections[1];
						Axis3D rightAxis = side1.GetAxis();
						Axis3D backAxis = side2.GetAxis();
						Axis3D upAxis = DirectionUtility.OtherAxis(rightAxis, backAxis);
						GeneralDirection3D up =
							upAxis == Axis3D.X ? xWall :
							upAxis == Axis3D.Y ? yWall :
							zWall;

						transformation = CubicTransformation.FromDirections(side1.Opposite(), up.Opposite(), side2.Opposite());
					}
					else
					{
						transformation = CubicTransformation.FromDirections(xWall, yWall.Opposite(), zWall);
					}
				}
			}
			else if (wallNeighborsCount >= 4)
			{
				stairType = StairShape.FullBlock;
			}

			extraVoxelData = SetStairType(extraVoxelData, stairType);
			voxel.CubicTransformation = transformation;
			voxel.extraData = extraVoxelData;
			map.SetVoxel(position, voxel);
		}


		protected sealed override void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions)
		{
			for (int i = 0; i < voxelPositions.Count; i++)
			{
				Vector3Int voxelPosition = voxelPositions[i];
				Voxel voxel = map.GetVoxel(voxelPosition);

				StairShape stairType = GetStairShape(voxel.extraData);

				if (isTransparent)
					voxel.OpenAllSide();
				else if (stairType == StairShape.FullBlock)
					voxel.CloseAllSide();
				else
				{
					CubicTransformation transformation = voxel.CubicTransformation;

					GeneralDirection3D globalUp = transformation.Up;
					GeneralDirection3D globalDown = globalUp.Opposite();
					GeneralDirection3D globalRight = transformation.Right;
					GeneralDirection3D globalLeft = globalRight.Opposite();
					GeneralDirection3D globalForward = transformation.Forward;
					GeneralDirection3D globalBack = globalForward.Opposite();

					voxel.SetSideClosed(globalUp, false);
					voxel.SetSideClosed(globalDown, true);
					voxel.SetSideClosed(globalRight, false);
					voxel.SetSideClosed(globalBack, false);

					bool leftClosed = stairType == StairShape.InnerCornerStair;
					bool freontClosed = stairType is StairShape.SimpleStair or StairShape.InnerCornerStair;

					voxel.SetSideClosed(globalLeft, leftClosed);
					voxel.SetSideClosed(globalForward, freontClosed);
				}

				map.SetVoxel(voxelPosition, voxel);
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


		void BuildMesh(VoxelMap map, Vector3Int position, MeshBuilder meshBuilder)
		{
			Voxel voxelData = map.GetVoxel(position);
			byte extraVoxelData = voxelData.extraData;
			StairShape stairShape = GetStairShape(extraVoxelData);
			CubicTransformation transformation = voxelData.CubicTransformation;
			bool autoSetup = GetAutoSetup(extraVoxelData);

			meshSetup.BuildMesh(position, voxelData.shapeId, stairShape, transformation, autoSetup, map, meshBuilder);
		}

		// ---------------------------------- Physical Mesh Generation ------------------------------------------------------------

		public override void GetPhysicalSides(List<Vector3[]> resultSides, VoxelMap map, Vector3Int voxelPoint)   // LEGACY?
		{
			Voxel voxel = map.GetVoxel(voxelPoint);
			StairShape stairType = GetStairShape(voxel.extraData);

			Matrix4x4 transformationMatrix =
				Matrix4x4.Translate(Vector3.one * 0.5f) *
				voxel.CubicTransformation.GetTransformationMatrix() *
				Matrix4x4.Translate(Vector3.one * -0.5f);

			Vector3[][] meshSetup = stairType switch
			{
				StairShape.SimpleStair => StairMeshSetup.simpleStair_PhysicalSides,
				StairShape.InnerCornerStair => StairMeshSetup.innerCorner_PhysicalSides,
				StairShape.OuterCornerStair => StairMeshSetup.outerCorner_PhysicalSides,
				_ => null,
			};

			if (meshSetup == null) return;

			for (int i = 0; i < meshSetup.Length; i++)
			{
				Vector3[] localSide = meshSetup[i];

				Vector3[] side = new Vector3[localSide.Length];
				for (int j = 0; j < localSide.Length; j++)
					side[j] = transformationMatrix.MultiplyPoint(localSide[j]) + voxelPoint;

				resultSides.Add(side);
			}
		}

		public sealed override void GetNavigationEdges(List<DirectedEdge> resultEdges, VoxelMap map, Vector3Int voxelPosition)
		{
			Voxel voxel = map.GetVoxel(voxelPosition);
			StairShape stairType = GetStairShape(voxel.extraData);
			CubicTransformation cubicTransform = voxel.CubicTransformation;

			AddFullSideNavigationEdges(resultEdges, map, voxelPosition, cubicTransform.Down);
			if (stairType is StairShape.FullBlock) return;
			if (stairType is StairShape.SimpleStair or StairShape.InnerCornerStair)
				AddFullSideNavigationEdges(resultEdges, map, voxelPosition, cubicTransform.Forward);
			if (stairType is StairShape.InnerCornerStair)
				AddFullSideNavigationEdges(resultEdges, map, voxelPosition, cubicTransform.Right);

			if (stairType is StairShape.SimpleStair)
				AddSimpleStairNavigationEdges(resultEdges, voxelPosition, cubicTransform);

			if (stairType is StairShape.SimpleStair or StairShape.OuterCornerStair)
				AddStairSideNavigationEdges(resultEdges, voxelPosition, cubicTransform.Right, cubicTransform.Back, cubicTransform.Up);

			if (stairType is StairShape.SimpleStair or StairShape.InnerCornerStair)
				AddStairSideNavigationEdges(resultEdges, voxelPosition, cubicTransform.Left, cubicTransform.Back, cubicTransform.Up);

			if (stairType is StairShape.OuterCornerStair)
				AddStairSideNavigationEdges(resultEdges, voxelPosition, cubicTransform.Forward, cubicTransform.Right, cubicTransform.Up);

			if (stairType is StairShape.InnerCornerStair)
				AddStairSideNavigationEdges(resultEdges, voxelPosition, cubicTransform.Back, cubicTransform.Right, cubicTransform.Up);


		}

		void AddSimpleStairNavigationEdges(List<DirectedEdge> resultEdges, Vector3Int voxelPosition, CubicTransformation transformation)
		{
			GeneralDirection3D forward = transformation.Forward;
			GeneralDirection3D up = transformation.Up;
			GeneralDirection3D right = transformation.Right;
			Vector3 center = Vector3.one * 0.5f + voxelPosition;
			Vector3 forwardVector = forward.ToVector();
			Vector3 upVector = up.ToVector();
			Vector3 rightVector = right.ToVector();
			Vector3 normal = upVector - forwardVector;
			Vector3 p1Vector = rightVector * 0.5f;
			Vector3 p2Vector = (upVector + forwardVector) / 2f;

			AddRectNavigationEdges(resultEdges, normal, p1Vector, p2Vector, center);
		}

		void AddStairSideNavigationEdges(List<DirectedEdge> resultEdges, Vector3Int voxelPosition, GeneralDirection3D normal, GeneralDirection3D forward, GeneralDirection3D up)
		{
			Vector3 normalVector = normal.ToVector();
			Vector3 voxelCenter = voxelPosition + Vector3.one * 0.5f;
			Vector3 sideCenter = voxelCenter + normalVector * 0.5f;
			Vector3 backVector = -forward.ToVector() *0.5f;
			Vector3 downVector = -up.ToVector() * 0.5f; 
			 
			resultEdges.Add(new(sideCenter, sideCenter + backVector, normalVector));
			resultEdges.Add(new(sideCenter, sideCenter + downVector, normalVector));
		}


		static void AddFullSideNavigationEdges(List<DirectedEdge> resultEdges, VoxelMap map, Vector3Int voxelPosition, GeneralDirection3D direction)
		{
			if (map.TryGetVoxel(voxelPosition + direction.ToVectorInt(), out Voxel neighbor))
			{
				if (neighbor.IsSideClosed(direction.Opposite())) return;
			}

			GeneralDirection3D perpendicular1 = direction.GetPerpendicularNext();
			GeneralDirection3D perpendicular2 = direction.GetPerpendicularPrevious();
			Vector3 normal = direction.ToVector();
			Vector3 p1Vector = perpendicular1.ToVector() * 0.5f;
			Vector3 p2Vector = perpendicular2.ToVector() * 0.5f;
			Vector3 center = Vector3.one * 0.5f + voxelPosition + normal * 0.5f;

			AddRectNavigationEdges(resultEdges, normal, p1Vector, p2Vector, center);
		}

		private static void AddRectNavigationEdges(List<DirectedEdge> resultEdges, Vector3 normal, Vector3 offset1, Vector3 offset2, Vector3 center)
		{
			resultEdges.Add(new(center, center + offset1, normal));
			resultEdges.Add(new(center, center + offset2, normal));
			resultEdges.Add(new(center, center - offset1, normal));
			resultEdges.Add(new(center, center - offset2, normal));

			resultEdges.Add(new(center, center + offset1 + offset2, normal));
			resultEdges.Add(new(center, center + offset1 - offset2, normal));
			resultEdges.Add(new(center, center - offset1 + offset2, normal));
			resultEdges.Add(new(center, center - offset1 - offset2, normal));
		}

		// EXTRA CONTROL METHODS ------------

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
			new ExtraVoxelControl<StairShape>()
			{
				name = "Stair Type",
				getValue = GetStairShape,
				setValue = SetStairType
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


		const int extraInfo_isAutoSet = 0;
		const int extraInfo_stairType = 1;
		const int extraInfo_level = 2;

		static bool GetAutoSetup(byte extraVoxelData) => extraVoxelData.Get2Bit(extraInfo_isAutoSet) == 0;
		static byte SetAutoSetup(byte originalExtraVoxelData, bool newValue)
		{
			byte newData = originalExtraVoxelData.Set2Bit(extraInfo_isAutoSet, newValue ? 0 : 1);
			return newData;
		}
		static StairShape GetStairShape(byte extraData) => (StairShape)extraData.Get2Bit(extraInfo_stairType);
		static byte SetStairType(byte originalExtraData, StairShape newValue) =>
			 originalExtraData.Set2Bit(extraInfo_stairType, (int)newValue);
		static int GetLevel(byte extraVoxelData) => extraVoxelData.Get2Bit(extraInfo_level);
		static byte SetLevel(byte originalExtraVoxelData, int newValue) =>
			originalExtraVoxelData.Set2Bit(extraInfo_level, newValue);
	}
}