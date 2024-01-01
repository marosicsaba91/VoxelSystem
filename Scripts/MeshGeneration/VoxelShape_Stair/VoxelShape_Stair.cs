using EasyInspector;
using MeshUtility;
using MUtility;
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

			StairShape stairType = GetStairType(extraVoxelData);
			CubicTransformation transformation = new(voxel.cubicTransformation);

			if (wallNeighborsCount == 1)
			{
				GeneralDirection3D down = wallNeighborsDirections[0];
				GeneralDirection3D up = down.Opposite();
				transformation.upDirection = up;
				if (stairNeighborsCount == 2)
				{
					GeneralDirection3D s1 = stairNeighborsDirections[0];
					GeneralDirection3D s2 = stairNeighborsDirections[1];
					if (s1 != s2.Opposite() && s1 != up && s2!= up)
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
			voxel.cubicTransformation = transformation.ToByte();
			voxel.extraData = extraVoxelData;
			map.SetVoxel(position, voxel);
		}


		protected sealed override void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions)
		{
			for (int i = 0; i < voxelPositions.Count; i++)
			{
				Vector3Int voxelPosition = voxelPositions[i];
				Voxel v = map.GetVoxel(voxelPosition);

				StairShape stairType = GetStairType(v.extraData);

				if (isTransparent)
					v.OpenAllSide();
				else if (stairType == StairShape.FullBlock)
					v.CloseAllSide();
				else
				{
					CubicTransformation transformation = new(v.cubicTransformation);

					GeneralDirection3D globalUp = transformation.Up;
					GeneralDirection3D globalDown = globalUp.Opposite();
					GeneralDirection3D globalRight = transformation.Right;
					GeneralDirection3D globalLeft = globalRight.Opposite();
					GeneralDirection3D globalForward = transformation.Forward;
					GeneralDirection3D globalBack = globalForward.Opposite();

					v.SetSideClosed(globalUp, false);
					v.SetSideClosed(globalDown, true);
					v.SetSideClosed(globalRight, false);
					v.SetSideClosed(globalBack, false);

					bool leftClosed = stairType == StairShape.InnerCornerStair;
					bool forwardClosed = stairType is StairShape.SimpleStair or StairShape.InnerCornerStair;

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


		void BuildMesh(VoxelMap map, Vector3Int position, MeshBuilder meshBuilder)
		{
			Voxel voxelData = map.GetVoxel(position);
			byte extraVoxelData = voxelData.extraData;
			StairShape stairShape = GetStairType(extraVoxelData);
			CubicTransformation transformation = new(voxelData.cubicTransformation);
			bool autoSetup = GetAutoSetup(extraVoxelData);

			meshSetup.BuildMesh(position, voxelData.shapeId, stairShape, transformation, autoSetup, map, meshBuilder);
		}

		// ---------------------------------- Physical Mesh Generation ------------------------------------------------------------

		public override void BuildPhysicalMeshSides(FlexibleMesh flexMesh, VoxelMap map, Vector3Int voxelPoint, ref int sideCounter)
		{
			// GenerateRotatedPhysical();
			/*
			Voxel voxel = map.GetVoxel(voxelPoint);
			CubicTransformation transformation = GetTransformation(voxel.extraData);
			StairShape stairType = GetStairType(voxel.extraData);

			GeneralDirection3D localBackInGlobal = transformation.TransformDirection(GeneralDirection3D.Back);
			byte transformIndex = transformation.ToByte();

			if (stairType == StairShape.SimpleStair)
			{
				flexMesh.AddFace(meshSetup.transformedSimpleStairs_Default[transformIndex], voxelPoint);

				// if(voxel.IsSideClosed(localBackInGlobal))
				flexMesh.AddFace(meshSetup.transformedBackSide_Physical[transformIndex], voxelPoint);

				if (voxel.IsSideClosed(GeneralDirection3D.Down))
					flexMesh.AddFace(meshSetup.transformedBottom_Physical[transformIndex], voxelPoint);
				return;
			}
			*/
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
				getValue = GetStairType,
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
		static StairShape GetStairType(byte extraData) => (StairShape)extraData.Get2Bit(extraInfo_stairType);
		static byte SetStairType(byte originalExtraData, StairShape newValue) =>
			 originalExtraData.Set2Bit(extraInfo_stairType, (int)newValue);
		static int GetLevel(byte extraVoxelData) => extraVoxelData.Get2Bit(extraInfo_level);
		static byte SetLevel(byte originalExtraVoxelData, int newValue) =>
			originalExtraVoxelData.Set2Bit(extraInfo_level, newValue);

		/*
		static ushort SetTransformation(ushort originalExtraData, CubicTransformation newValue) =>
			originalExtraData.SetByte(0, newValue.ToByte());

		static GeneralDirection3D GetUpDirection(ushort extraVoxelData) => GetTransformation(extraVoxelData).upDirection;

		static ushort SetUpDirection(ushort originalExtraValue, GeneralDirection3D value)
		{
			CubicTransformation cubicTransformation = new((byte)originalExtraValue)
			{
				upDirection = value
			};
			return originalExtraValue.SetByte(0, cubicTransformation.ToByte());
		}
		static int GetRotation(ushort extraVoxelData) => GetTransformation(extraVoxelData).verticalRotation;
		static ushort SetRotation(ushort originalExtraValue, int value)
		{
			value %= 4;
			CubicTransformation cubicTransformation = new((byte)originalExtraValue)
			{
				verticalRotation = value
			};
			return originalExtraValue.SetByte(0, cubicTransformation.ToByte());
		}

		static bool GetFlip(ushort extraVoxelData) => GetTransformation(extraVoxelData).isVerticalFlipped;

		static ushort SetFlip(ushort originalExtraValue, bool value)
		{
			CubicTransformation cubicTransformation = new((byte)originalExtraValue)
			{
				isVerticalFlipped = value
			};
			return originalExtraValue.SetByte(0, cubicTransformation.ToByte());
		}
		*/
	}
}