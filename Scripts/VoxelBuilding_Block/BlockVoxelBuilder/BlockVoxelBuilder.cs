using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	struct BlockCalculationSetting
	{
		public bool mergeCloseEdges;
		public bool openOnSides;
		public bool showFacesBetweenMaterials;
	}

	public static class BlockVoxelBuilder
	{
		internal static void BuildMeshFromBlocks(VoxelBlockLibrary blockLibrary, List<Block> blocksList,
			List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
		{
			foreach (Block block in blocksList)
			{
				if (!blockLibrary.TryGetMesh(block, out CustomMesh mesh))
					continue;
				Vector3 offset = block.Center;
				vertices.AddRange(mesh.vertices.Select(v => v + offset));
				normals.AddRange(mesh.normals);
				uv.AddRange(mesh.uv);
				triangles.AddRange(mesh.triangles.Select(t => t + vertices.Count - mesh.vertices.Length));
			}
		}

		static List<Vector3> breakPoints = new();
		public static List<(Vector3Int,Vector3Int)> possibleCorners = new();
		static BlockCalculationSetting blockSetup = new();
		static VoxelMap voxelMap;
		static Vector3Int size;


		internal static void CalculateBlocks(VoxelMap map, List<List<Block>> blocksByMaterial, BlockCalculationSetting setup)
		{
			possibleCorners.Clear();
			breakPoints.Clear();
			voxelMap = map;
			size = map.FullSize;
			blockSetup = setup;

			int materialCount = blocksByMaterial.Count;

			for (int x = size.x - 1; x >= 0; x--)
				for (int y = size.y - 1; y >= 0; y--)
					for (int z = size.z - 1; z >= 0; z--)
					{
						int voxelValue = map.GetVoxel(x, y, z);
						if (voxelValue.IsEmpty()) continue;
						int materialIndex = (voxelValue >= materialCount) ? materialCount - 1 : voxelValue;
						blocksByMaterial[materialIndex].AddRange(VoxelToBlocks(voxelValue, x, y, z));
					}
		}


		// Methods
		static IEnumerable<Block> VoxelToBlocks(int voxelValue, int vXi, int vYi, int vZi) // Voxel Index
		{
			for (int dX = -1; dX <= 1; dX += 2) // Sub-Voxels
				for (int dY = -1; dY <= 1; dY += 2)
					for (int dZ = -1; dZ <= 1; dZ += 2)
						foreach (Block b in SubVoxelToBlock(voxelValue, vXi, vYi, vZi, dX, dY, dZ))
							yield return b;
		}

		static IEnumerable<Block> SubVoxelToBlock(int voxelValue, int vXi, int vYi, int vZi, int dX, int dY, int dZ)
		{
			int nXi = vXi + dX; // Neighbour Voxel Index
			int nYi = vYi + dY;
			int nZi = vZi + dZ;

			Vector3Int voxelIndex = new(vXi, vYi, vZi);
			Vector3Int subVoxelDir = new(dX, dY, dZ);

			bool nXf = GetAnyNeighbour(voxelValue, nXi, vYi, vZi, out bool f1);
			bool nYf = GetAnyNeighbour(voxelValue, vXi, nYi, vZi, out bool f2);
			bool nZf = GetAnyNeighbour(voxelValue, vXi, vYi, nZi, out bool f3);
			bool nXYf = GetAnyNeighbour(voxelValue, nXi, nYi, vZi, out bool f4);
			bool nYZf = GetAnyNeighbour(voxelValue, vXi, nYi, nZi, out bool f5);
			bool nZXf = GetAnyNeighbour(voxelValue, nXi, vYi, nZi, out bool f6);
			bool nXYZf = GetAnyNeighbour(voxelValue, nXi, nYi, nZi, out bool f7);

			// ---------------------------------------------------------------------------------------------

			int neighbourCount = (nXf ? 1 : 0) + (nYf ? 1 : 0) + (nZf ? 1 : 0);
			int crossNeighbourCount = (nXYf ? 1 : 0) + (nYZf ? 1 : 0) + (nZXf ? 1 : 0);
			int crossNeighbour2Count = (nXYf ? 1 : 0) + (nYZf ? 1 : 0) + (nZXf ? 1 : 0) + (nXYZf ? 1 : 0);

			if (blockSetup.showFacesBetweenMaterials)
			{
				if (neighbourCount == 3 && crossNeighbour2Count == 4) // IS FULLY FILLED
					yield break;
			}
			else
			{
				if (f1 && f2 && f3 && f4 && f5 && f6 && f7) // IS FULLY FILLED or BETWEEN TWO MATERIALS
					yield break;
			}


			// IS FILLED

			if (neighbourCount == 0) // CORNER // ----------------------------------------------------------------------------
			{
				yield return new Block(BlockType.CornerPositive, voxelIndex, subVoxelDir, Vector3Int.one);

				if (crossNeighbourCount != 0 && blockSetup.mergeCloseEdges) // BREAKING POINT IN MODEL
				{
					Axis3D axis = nXYf ? Axis3D.Z : nYZf ? Axis3D.X : Axis3D.Y;
					Vector3 normalVector = axis.ToVector().MultiplyAllAxis(subVoxelDir);
					breakPoints.Add(voxelIndex + (Vector3)subVoxelDir * 0.25f - normalVector * 0.5f);
				}
			}
			else if (neighbourCount == 3) // ----------------------------------------------------------------------------
			{
				if (crossNeighbourCount == 0) // CROSS
				{
					yield return new Block(BlockType.CrossCorner, voxelIndex, subVoxelDir, Vector3Int.one);
				}
				else if (crossNeighbourCount == 2 && !nXYZf) // EDGE NEGATIVE
				{
					Axis3D axis = !nXYf ? Axis3D.Z : !nYZf ? Axis3D.X : Axis3D.Y;
					yield return new Block(BlockType.EdgeNegative, voxelIndex, subVoxelDir, Vector3Int.one, axis);
				}
				else if (crossNeighbourCount == 3) // CORNER NEGATIVE
				{
					yield return new Block(BlockType.CornerNegative, voxelIndex, subVoxelDir, Vector3Int.one);

					if (crossNeighbourCount != 3 && !blockSetup.mergeCloseEdges) // BREAKING POINT IN MODEL
					{
						Axis3D axis = !nXYf ? Axis3D.Z : !nYZf ? Axis3D.X : Axis3D.Y;
						Vector3 normalVector = axis.ToVector().MultiplyAllAxis(subVoxelDir);
						breakPoints.Add(voxelIndex + (Vector3)subVoxelDir * 0.25f - normalVector * 0.5f);
					}
				}
			}

			var normal = new Vector3Int
			{
				x = nXf ? 0 : dX,
				y = nYf ? 0 : dY,
				z = nZf ? 0 : dZ
			};

			if (neighbourCount == 2) // -------------------------- SIDE ---------------------------------------------
			{
				if (crossNeighbour2Count == 0) // SIDE TO EDGE NEGATIVE
				{
					Axis3D axis = normal.ToAxis();
					yield return new Block(BlockType.SideToNegativeEdge, voxelIndex, subVoxelDir, Vector3Int.one, axis);
					yield break;
				}

				Vector3Int inPlaneNeighbourIndex = voxelIndex + subVoxelDir - normal;
				bool inPlaneNeighbour = GetAnyNeighbour(voxelValue, inPlaneNeighbourIndex, out bool _);

				if (crossNeighbour2Count == 1 && inPlaneNeighbour) // SIDE
				{
					Axis3D axis = normal.ToAxis();
					yield return new Block(BlockType.SidePositive, voxelIndex, subVoxelDir, Vector3Int.one, axis);
				}

				else if (crossNeighbourCount == 3 || (crossNeighbourCount == 2 && ! inPlaneNeighbour)) // POSSIBLE NEGATIVE CORNERS
				{
					possibleCorners.Add((voxelIndex + normal, subVoxelDir - normal * 2));
				}
			}
			else if (neighbourCount == 1) // ----------------------------------------------------------------------------
			{
				if (crossNeighbourCount == 0) // EDGE
				{
					Axis3D axis = Negate(normal);
					yield return new Block(BlockType.EdgePositive, voxelIndex, subVoxelDir, Vector3Int.one, axis);
				}
				else if (crossNeighbourCount == 1)
				{
					if (nXYZf) // EDGE TO EDGE
					{
						Vector3Int nIndex = voxelIndex + normal;
						bool middleNeighbourFilled = GetAnyNeighbour(voxelValue, nIndex.x, nIndex.y, nIndex.z, out bool _);
						if (!middleNeighbourFilled)
						{
							Axis3D axis = Negate(normal);
							SeparateVector(normal, out Vector3Int normal1, out Vector3Int normal2);
							Vector3Int crossEdgeIndex1 = voxelIndex + subVoxelDir - normal + normal1;
							bool crossEdgeFilled1 = GetAnyNeighbour(voxelValue, crossEdgeIndex1.x, crossEdgeIndex1.y, crossEdgeIndex1.z, out bool _);
							Axis3D connectingAxis = crossEdgeFilled1 ? normal2.ToAxis() : normal1.ToAxis();
							if (axis == Axis3D.X && connectingAxis != Axis3D.Y)
								yield break;
							if (axis == Axis3D.Y && connectingAxis != Axis3D.Z)
								yield break;
							if (axis == Axis3D.Z && connectingAxis != Axis3D.X)
								yield break;
							yield return new Block(BlockType.EdgeToEdge, voxelIndex, subVoxelDir, Vector3Int.one, axis);
						}
					}
					if (!blockSetup.mergeCloseEdges || !nXYZf)  // EDGE
					{
						Axis3D axis = Negate(normal);
						Vector3Int nIndex = voxelIndex + normal;
						bool crossEdgeFilled = GetAnyNeighbour(voxelValue, nIndex.x, nIndex.y, nIndex.z, out bool _);
						if (crossEdgeFilled)
						{
							yield return new Block(BlockType.EdgePositive, voxelIndex, subVoxelDir, Vector3Int.one, axis);
							yield break;
						}
					}
				}

				// MERGE CLOSE EDGES TEST
				//if (neighbourCount == 2 && crossNeighbourCount == 2 && (mergeCloseEdges || nXYZf))   // EDGE NEGATIVE
				//{
				//	Axis3D axis = Negate(normal);
				//	Vector3Int crossEdgePosition = voxelIndex - normal;
				//	bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgePosition);
				//	if (!crossEdgeFilled)
				//	{
				//		yield return new Block(BlockType.EdgeNegative, inVoxelDir, blockSize, axis, blockPosition);
				//	}
				//}


				else if (crossNeighbourCount == 2 && nXYZf) // EDGE TO SIDE
				{
					Axis3D axis = Negate(normal);
					Vector3Int crossEdgeIndex = voxelIndex + normal;
					bool crossEdgeFilled = GetAnyNeighbour(voxelValue, crossEdgeIndex, out bool _);
					if (!crossEdgeFilled)
					{
						yield return new Block(BlockType.SideToPositiveEdge, voxelIndex, subVoxelDir, Vector3Int.one, axis);
					}
				}
			}
		}


		static bool GetAnyNeighbour(int currentValue, Vector3Int index, out bool filled) => GetAnyNeighbour(currentValue, index.x, index.y, index.z, out filled);

		static bool GetAnyNeighbour(int currentValue, int x, int y, int z, out bool filled)
		{
			// if Neighbour Exists
			int val;
			if (x >= 0 && x < voxelMap.FullSize.x &&
				y >= 0 && y < voxelMap.FullSize.y &&
				z >= 0 && z < voxelMap.FullSize.z)
			{
				val = voxelMap.GetVoxel(x, y, z);
				filled = val.IsFilled();
				return val == currentValue;
			}

			// Neighbour Do NOT Exists:

			if (!blockSetup.openOnSides)
			{
				filled = false;
				return false;
			}

			Vector3Int size = voxelMap.FullSize;
			int xin = Mathf.Clamp(x, 0, size.x - 1);
			int yin = Mathf.Clamp(y, 0, size.y - 1);
			int zin = Mathf.Clamp(z, 0, size.z - 1);

			val = voxelMap.GetVoxel(xin, yin, zin);
			filled = val.IsFilled();
			return val == currentValue;
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