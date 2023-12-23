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


		protected sealed override bool IsInitialized => meshSetup.IsInitialized;
		protected override void InitializeCachedData() =>
			meshSetup.Initialize(cubeTextureCoordinates, overrideTextureCoordinates);

		// ---------------------------------------------------------------------------------------------------------------

		protected override void SetupVoxelData(VoxelMap map, List<Vector3Int> voxelPositions, int shapeIndex)
		{
			for (int i = 0; i < voxelPositions.Count; i++)
				SetupVoxelTypeAndTransformation(map, voxelPositions[i], shapeIndex);
		}

		void SetupVoxelTypeAndTransformation(VoxelMap map, Vector3Int position, int shapeIndex)
		{
			Voxel voxelData = map.GetVoxel(position);
			ushort extraVoxelData = voxelData.extraVoxelData;
			bool useAutoSetup = GetAutoSetup(extraVoxelData);
			if (!useAutoSetup) return;

			// SetupFromMesh Rotation & StairShape
			int wallNeighborsCount = 0;
			int stairNeighborsCount = 0;

			foreach (GeneralDirection3D direction in DirectionUtility.generalDirection3DValues)
			{
				Vector3Int directionVector = direction.ToVectorInt();

				if (map.TryGetVoxel(position + directionVector, out Voxel neighbour))
				{
					if (neighbour.shapeId == shapeIndex)
					{
						stairNeighborsDirections[stairNeighborsCount] = direction;
						stairNeighborsCount++;
						if (stairNeighborsCount == 4)
							break;
					}
					else if (neighbour.IsFilled())
					{
						wallNeighborsDirections[stairNeighborsCount] = direction;
						wallNeighborsCount++;
						if (stairNeighborsCount == 3)
							break;
					}
				}
			}


			StairShape stairType = GetStairType(extraVoxelData);
			CubicTransformation transformation = GetTransformation(extraVoxelData);

			if (wallNeighborsCount == 1)
			{
				GeneralDirection3D down = wallNeighborsDirections[0];
				GeneralDirection3D up = down.Opposite();
				transformation.upDirection = up;
				if (stairNeighborsCount == 2)
				{
					GeneralDirection3D s1 = stairNeighborsDirections[0];
					GeneralDirection3D s2 = stairNeighborsDirections[1];
					if (s1 == s2.Opposite())
					{
						stairType = StairShape.SimpleStair;
						transformation = CubicTransformation.FromRightUp(s1, up);
					}
					else
					{
						stairType = StairShape.OuterCornerStair;
						transformation = CubicTransformation.FromRightForward(s1, s2);
					}
				}
			}
			else if (wallNeighborsCount == 2)
			{
				stairType = StairShape.SimpleStair;
				GeneralDirection3D down = wallNeighborsDirections[0];
				GeneralDirection3D back = wallNeighborsDirections[1];
				GeneralDirection3D up = down.Opposite();
				GeneralDirection3D forward = back.Opposite();
				if (up.GetAxis() != forward.GetAxis())
					transformation = CubicTransformation.FromUpForward(up, forward);
			}
			else if (wallNeighborsCount == 3)
			{
				stairType = StairShape.InnerCornerStair;
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
			else if (wallNeighborsCount >= 4)
			{
				stairType = StairShape.FullBlock;
			}

			extraVoxelData = SetStairType(extraVoxelData, stairType);
			extraVoxelData = SetTransformation(extraVoxelData, transformation);
			voxelData.extraVoxelData = extraVoxelData;
			map.SetVoxel(position, voxelData);
		}

		protected sealed override void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions)
		{
			for (int i = 0; i < voxelPositions.Count; i++)
			{
				Vector3Int voxelPosition = voxelPositions[i];
				Voxel v = map.GetVoxel(voxelPosition);

				StairShape stairType = GetStairType(v.extraVoxelData);

				if (isTransparent || stairType == StairShape.FullBlock)
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


		readonly GeneralDirection3D[] stairNeighborsDirections = new GeneralDirection3D[4];
		readonly GeneralDirection3D[] wallNeighborsDirections = new GeneralDirection3D[3];


		void BuildMesh(VoxelMap map, Vector3Int position, MeshBuilder meshBuilder)
		{
			Voxel voxelData = map.GetVoxel(position);
			ushort extraVoxelData = voxelData.extraVoxelData;
			StairShape stairShape = GetStairType(extraVoxelData);
			CubicTransformation transformation = GetTransformation(extraVoxelData);
			bool autoSetup = GetAutoSetup(extraVoxelData);

			meshSetup.BuildMesh(position, voxelData.shapeId, stairShape, transformation, autoSetup, map, meshBuilder);
		}

		// ---------------------------------- Physical Mesh Generation ------------------------------------------------------------

		public override void BuildPhysicalMeshSides(FlexibleMesh flexMesh, VoxelMap map, Vector3Int voxelPoint, ref int sideCounter)
		{
			// GenerateRotatedPhysical();
			/*
			Voxel voxel = map.GetVoxel(voxelPoint);
			CubicTransformation transformation = GetTransformation(voxel.extraVoxelData);
			StairShape stairType = GetStairType(voxel.extraVoxelData);

			GeneralDirection3D localBackInGlobal = transformation.TransformDirection(GeneralDirection3D.Back);
			byte transformIndex = transformation.GetIndex();

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

		static StairShape GetStairType(ushort voxelData) => (StairShape)voxelData.Get2Bit(extraInfo_stairType);
		static ushort SetStairType(ushort originalExtraVoxelData, StairShape newValue) =>
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
	}
}