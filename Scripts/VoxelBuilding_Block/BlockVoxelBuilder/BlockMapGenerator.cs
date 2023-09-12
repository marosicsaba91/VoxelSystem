using System;
using System.Collections.Generic;
using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public struct BlockGenerationSetting
	{
		public bool mergeCloseEdges;
		public bool openOnSides;
		public bool showFacesBetweenMaterials;
	}

	struct NeighbourData
	{
		public bool isSame;
		public bool isFilled;
	}


	public static class BlockMapGenerator
	{
		public static BlockGenerationSetting blockSetup = new();
		 
		static Dictionary<Vector3Int, Block> _currentMaterialBlocks = new();
		static readonly NeighbourData[,,] neighbours = new NeighbourData[3, 3, 3];

		static VoxelMap _currentVoxelMap;
		static Vector3Int _currentMapSize;

		static readonly List<Vector3> _breakPoints = new();

		public static void CalculateBlocks(List<Dictionary<Vector3Int, Block>> blockByMaterial, VoxelMap map, int materialCount)
		{
			while (blockByMaterial.Count < materialCount)
				blockByMaterial.Add(new());

			for (int i = 0; i < materialCount; i++)
			{
				blockByMaterial[i].Clear();
			}

			_breakPoints.Clear();

			_currentVoxelMap = map;
			_currentMapSize = map.FullSize;


			for (int vXi = _currentMapSize.x - 1; vXi >= 0; vXi--)
				for (int vYi = _currentMapSize.y - 1; vYi >= 0; vYi--)
					for (int vZi = _currentMapSize.z - 1; vZi >= 0; vZi--)
					{
						int voxelValue = map.GetVoxel(vXi, vYi, vZi);
						if (voxelValue.IsEmpty()) continue;

						for (int dX = -1; dX <= 1; dX += 1) // Sub-Voxels
							for (int dY = -1; dY <= 1; dY += 1)
								for (int dZ = -1; dZ <= 1; dZ += 1)
								{
									NeighbourData n = GetAnyNeighbour(voxelValue, vXi + dX, vYi + dY, vZi + dZ);
									neighbours[1 + dX, 1 + dY, 1 + dZ] = n;
								}


						int materialIndex = (voxelValue >= materialCount) ? materialCount - 1 : voxelValue;
						_currentMaterialBlocks = blockByMaterial[materialIndex];

						for (int dX = -1; dX <= 1; dX += 2) // Sub-Voxels
							for (int dY = -1; dY <= 1; dY += 2)
								for (int dZ = -1; dZ <= 1; dZ += 2)
									SubVoxelToBlock(vXi, vYi, vZi, dX, dY, dZ);
					} 
		}

		static NeighbourData GetAnyNeighbour(int currentValue, int x, int y, int z)
		{
			// if Neighbour Exists
			int neighbourValue;
			if (x >= 0 && x < _currentVoxelMap.FullSize.x &&
				y >= 0 && y < _currentVoxelMap.FullSize.y &&
				z >= 0 && z < _currentVoxelMap.FullSize.z)
			{
				neighbourValue = _currentVoxelMap.GetVoxel(x, y, z);
				return new NeighbourData() { isSame = neighbourValue == currentValue, isFilled = neighbourValue.IsFilled() };
			}

			// Neighbour Do NOT Exists:
			if (!blockSetup.openOnSides)
				return new NeighbourData() { isSame = false, isFilled = false };

			Vector3Int size = _currentVoxelMap.FullSize;
			int xin = Mathf.Clamp(x, 0, size.x - 1);
			int yin = Mathf.Clamp(y, 0, size.y - 1);
			int zin = Mathf.Clamp(z, 0, size.z - 1);

			neighbourValue = _currentVoxelMap.GetVoxel(xin, yin, zin);
			return new NeighbourData() { isSame = neighbourValue == currentValue, isFilled = neighbourValue.IsFilled() };
		}

		static void SubVoxelToBlock(int vXi, int vYi, int vZi, int dX, int dY, int dZ)
		{
			NeighbourData nX = neighbours[1 + dX, 1, 1];
			NeighbourData nY = neighbours[1, 1 + dY, 1];
			NeighbourData nZ = neighbours[1, 1, 1 + dZ];
			NeighbourData nXY = neighbours[1 + dX, 1 + dY, 1];
			NeighbourData nYZ = neighbours[1, 1 + dY, 1 + dZ];
			NeighbourData nZX = neighbours[1 + dX, 1, 1 + dZ];
			NeighbourData nXYZ = neighbours[1 + dX, 1 + dY, 1 + dZ];

			if (blockSetup.showFacesBetweenMaterials)
			{
				if (nX.isSame && nY.isSame && nZ.isSame && nXY.isSame && nYZ.isSame && nZX.isSame && nXYZ.isSame) // IS FULLY FILLED
					return;
			}
			else
			{
				if (nX.isFilled && nY.isFilled && nZ.isFilled && nXY.isFilled && nYZ.isFilled && nZX.isFilled && nXYZ.isFilled) // IS FULLY FILLED or BETWEEN TWO MATERIALS
					return;
			}

			int neighbourCount = (nX.isSame ? 1 : 0) + (nY.isSame ? 1 : 0) + (nZ.isSame ? 1 : 0);
			int crossNeighbourCount = (nXY.isSame ? 1 : 0) + (nYZ.isSame ? 1 : 0) + (nZX.isSame ? 1 : 0);
			int crossNeighbour2Count = (nXY.isSame ? 1 : 0) + (nYZ.isSame ? 1 : 0) + (nZX.isSame ? 1 : 0) + (nXYZ.isSame ? 1 : 0);

			// ---------------------------------------------------------------------------------------------


			Vector3Int voxelIndex = new(vXi, vYi, vZi);
			Vector3Int subVoxel = new(dX, dY, dZ);
			Vector3Int subVoxelIndex = (voxelIndex * 2) + (subVoxel + Vector3Int.one) / 2;

			// Is Filled On The Edge ---------------------------------------------------------------------------------------------

			if (neighbourCount == 0) // CORNER // ----------------------------------------------------------------------------
			{
				_currentMaterialBlocks.TryAdd(subVoxelIndex, new Block(BlockType.CornerPositive, subVoxel));

				if (crossNeighbourCount != 0 && blockSetup.mergeCloseEdges) // BREAKING POINT IN MODEL
				{
					Axis3D axis = nXY.isSame ? Axis3D.Z : nYZ.isSame ? Axis3D.X : Axis3D.Y;
					Vector3 normalVector = axis.ToVector().MultiplyAllAxis(subVoxel);
					_breakPoints.Add(voxelIndex + (Vector3)subVoxel * 0.25f - normalVector * 0.5f);
				}
				return;
			}

			if (neighbourCount == 3) // CROSS // ----------------------------------------------------------------------------
			{
				if (crossNeighbourCount == 0) // CROSS
				{
					_currentMaterialBlocks.Add(subVoxelIndex, new Block(BlockType.CrossCorner, subVoxel));
				}
				return;
			}

			Vector3Int normal = new()
			{
				x = nX.isSame ? 0 : dX,
				y = nY.isSame ? 0 : dY,
				z = nZ.isSame ? 0 : dZ
			};

			if (neighbourCount == 2) // SIDE // -----------------------------------------------------------------------------------
			{

				if (crossNeighbourCount == 0) // SIDE TO EDGE NEGATIVE
				{
					Axis3D axis = normal.ToAxis();
					_currentMaterialBlocks.Add(subVoxelIndex, new Block(BlockType.SideToNegativeEdge, subVoxel, axis));
					return;
				}

				Vector3Int inPlaneNeighbourIndex = Vector3Int.one + subVoxel - normal;
				NeighbourData inPlaneNeighbour = neighbours[inPlaneNeighbourIndex.x, inPlaneNeighbourIndex.y, inPlaneNeighbourIndex.z];

				if (crossNeighbour2Count == 1 && inPlaneNeighbour.isSame) // SIDE
				{
					Axis3D axis = normal.ToAxis();
					_currentMaterialBlocks.Add(subVoxelIndex, new Block(BlockType.SidePositive, subVoxel, axis));
				}

				else if (crossNeighbourCount == 3 || (crossNeighbourCount == 2 && !inPlaneNeighbour.isSame)) // NEGATIVE CORNERS
				{
					subVoxel -= normal * 2;
					_currentMaterialBlocks.AddOrChangeValue(subVoxelIndex +normal, new Block(BlockType.CornerNegative, subVoxel));
					return;
				}

				bool negativeEdge =
					(crossNeighbourCount == 2 && nXYZ.isSame) || //  NEGATIVE EDGE (COMMON)
					(crossNeighbourCount == 1 && !inPlaneNeighbour.isSame && nXYZ.isSame); //  NEGATIVE EDGE (COLUMNS - Different Size)

				if (negativeEdge) // NEGATIVE EDGE
				{

					SeparateVector(subVoxel - normal, out Vector3Int normal1, out Vector3Int normal2);
					Vector3Int crossEdgeIndex = Vector3Int.one + normal + normal1;
					NeighbourData crossEdgeNeighbour = neighbours[crossEdgeIndex.x, crossEdgeIndex.y, crossEdgeIndex.z]; 
					subVoxel -= normal * 2;
					Axis3D axis = crossEdgeNeighbour.isSame ? normal2.ToAxis() : normal1.ToAxis();
					_currentMaterialBlocks.TryAdd(subVoxelIndex + normal, new Block(BlockType.EdgeNegative, subVoxel, axis));
				}

				return;
			}
			else if (neighbourCount == 1) // EDGE // --------------------------------------------------------------------------
			{
				Axis3D axis = Negate(normal);

				if (crossNeighbourCount == 0) // EDGE
				{
					_currentMaterialBlocks.Add(subVoxelIndex, new Block(BlockType.EdgePositive, subVoxel, axis));
					return;
				}

				Vector3Int crossEdgeIndex = Vector3Int.one + normal;
				NeighbourData crossEdgeNeighbour = neighbours[crossEdgeIndex.x, crossEdgeIndex.y, crossEdgeIndex.z];


				bool negativeEdge =
					(crossEdgeNeighbour.isSame && blockSetup.mergeCloseEdges && nXYZ.isSame) ||  // NEGATIVE EDGE (COLUMNS - Merge)
					(crossEdgeNeighbour.isSame && crossNeighbourCount == 2 && nXYZ.isSame);      // NEGATIVE EDGE (COLUMNS - Different Size)

				if (negativeEdge) // NEGATIVE EDGE
				{
					SeparateVector(normal, out Vector3Int normal1, out _); 
					subVoxel -= normal1 * 2;
					_currentMaterialBlocks.TryAdd(subVoxelIndex + normal1, new Block(BlockType.EdgeNegative, subVoxel, axis));
					return;
				}

				if (crossNeighbourCount == 1)
				{
					if (crossEdgeNeighbour.isSame)
					{
						if (!blockSetup.mergeCloseEdges || !nXYZ.isSame) // EDGE						
							_currentMaterialBlocks.Add(subVoxelIndex, new Block(BlockType.EdgePositive, subVoxel, axis));
					}
					else
					{
						if (nXYZ.isSame) // EDGE TO EDGE
						{
							SeparateVector(normal, out Vector3Int normal1, out Vector3Int normal2);
							crossEdgeIndex = Vector3Int.one + subVoxel - normal + normal1;
							crossEdgeNeighbour = neighbours[crossEdgeIndex.x, crossEdgeIndex.y, crossEdgeIndex.z];
							Axis3D connectingAxis = crossEdgeNeighbour.isSame ? normal2.ToAxis() : normal1.ToAxis();
							if (axis == Axis3D.X && connectingAxis != Axis3D.Y)
								return;
							if (axis == Axis3D.Y && connectingAxis != Axis3D.Z)
								return;
							if (axis == Axis3D.Z && connectingAxis != Axis3D.X)
								return;
							_currentMaterialBlocks.Add(subVoxelIndex, new Block(BlockType.EdgeToEdge, subVoxel, axis));
						}
					}
					return;
				}


				if (crossNeighbourCount == 2 && nXYZ.isSame && !crossEdgeNeighbour.isSame) // EDGE TO SIDE
				{
					_currentMaterialBlocks.Add(subVoxelIndex, new Block(BlockType.SideToPositiveEdge, subVoxel, axis));
				}
			}
		}

		static void SeparateVector(Vector3Int vector, out Vector3Int a, out Vector3Int b)
		{
			if (vector.x == 0)
			{
				a = new Vector3Int(0, vector.y, 0);
				b = new Vector3Int(0, 0, vector.z);
			}
			else if (vector.y == 0)
			{
				a = new Vector3Int(vector.x, 0, 0);
				b = new Vector3Int(0, 0, vector.z);
			}
			else
			{
				a = new Vector3Int(vector.x, 0, 0);
				b = new Vector3Int(0, vector.y, 0);
			}
		}

		static Axis3D Negate(Vector3 v)
		{
			float x = v.x == 0 ? 1 : 0;
			float y = v.y == 0 ? 1 : 0;
			float z = v.z == 0 ? 1 : 0;
			return new Vector3(x, y, z).ToAxis();
		}
	}
}