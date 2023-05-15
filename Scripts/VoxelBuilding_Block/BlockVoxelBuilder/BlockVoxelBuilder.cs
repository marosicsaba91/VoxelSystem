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
		public bool separateVoxelMaterials;
		public bool openOnSides;
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

		static BlockCalculationSetting blockSetup = new ();

		internal static void CalculateBlocks(VoxelMap voxelMap, List<List<Block>> blocksByMaterial, BlockCalculationSetting setup)
		{
			Vector3Int size = voxelMap.FullSize;
			blockSetup = setup;

			int materialCount = blocksByMaterial.Count;

			for (int x = size.x - 1; x >= 0; x--)
				for (int y = size.y - 1; y >= 0; y--)
					for (int z = size.z - 1; z >= 0; z--)
					{
						int voxelValue = voxelMap.GetVoxel(x, y, z);
						if (voxelValue.IsEmpty()) continue;
						int materialIndex = (voxelValue >= materialCount) ? materialCount - 1 : voxelValue;
						blocksByMaterial[materialIndex].AddRange(VoxelToBlocks(voxelMap, voxelValue, x, y, z));
					}
		}

		// Methods
		static IEnumerable<Block> VoxelToBlocks(VoxelMap voxelMap, int voxelValue, int vXi, int vYi, int vZi) // Voxel Index
		{
			var voxelIndex = new Vector3Int(vXi, vYi, vZi);

			Vector3Int blockSize = Vector3Int.one;

			for (int dX = -1; dX <= 1; dX += 2) // In Voxel Direction
				for (int dY = -1; dY <= 1; dY += 2)
					for (int dZ = -1; dZ <= 1; dZ += 2)
					{
						int nXi = vXi + dX; // Neighbour Voxel Index
						int nYi = vYi + dY;
						int nZi = vZi + dZ;
						Vector3Int inVoxelDir = new(dX, dY, dZ);

						var blockPosition = new Vector3(
							vXi + (dX + 1) * 0.25f,
							vYi + (dY + 1) * 0.25f,
							vZi + (dZ + 1) * 0.25f);

						bool nXe = nXi >= 0 && nXi < voxelMap.FullSize.x; // Do Neighbour Row Exist
						bool nYe = nYi >= 0 && nYi < voxelMap.FullSize.y;
						bool nZe = nZi >= 0 && nZi < voxelMap.FullSize.z;

						bool nXf =   GetNeighbour(voxelMap, voxelValue, nXi, vYi, vZi, nXe);
						bool nYf =   GetNeighbour(voxelMap, voxelValue, vXi, nYi, vZi, nYe);
						bool nZf =   GetNeighbour(voxelMap, voxelValue, vXi, vYi, nZi, nZe); 
						bool nXYf = GetCrossNeighbour(voxelMap, voxelValue, nXi, nYi, vZi, nXe && nYe);
						bool nYZf = GetCrossNeighbour(voxelMap, voxelValue, vXi, nYi, nZi, nYe && nZe);
						bool nZXf = GetCrossNeighbour(voxelMap, voxelValue, nXi, vYi, nZi, nZe && nXe);
						bool nXYZf = GetCrossNeighbour(voxelMap, voxelValue, nXi, nYi, nZi, nXe && nYe && nZe);
						 

						// ---------------------------------------------------------------------------------------------

						int neighbourCount = (nXf ? 1 : 0) + (nYf ? 1 : 0) + (nZf ? 1 : 0);
						int crossNeighbourCount = (nXYf ? 1 : 0) + (nYZf ? 1 : 0) + (nZXf ? 1 : 0);
						int crossNeighbour2Count = (nXYf ? 1 : 0) + (nYZf ? 1 : 0) + (nZXf ? 1 : 0) + (nXYZf ? 1 : 0);

						if (neighbourCount == 3 && crossNeighbour2Count == 4)
							continue;

						var normal = new Vector3Int();
						Axis3D axis;

						// IS FILLED

						if (neighbourCount == 0) // CORNER
						{
							yield return new Block(BlockType.CornerPositive, inVoxelDir, blockSize, blockPosition);

							if (crossNeighbourCount != 0 && blockSetup.mergeCloseEdges) // BREAKING POINT IN MODEL
							{
								axis = nXYf ? Axis3D.Z : nYZf ? Axis3D.X : Axis3D.Y;
								Vector3 normalVector = axis.ToVector().MultiplyAllAxis(inVoxelDir);
								blockPosition += (Vector3)inVoxelDir * 0.25f - normalVector * 0.5f;
								yield return new Block(BlockType.BreakPoint, inVoxelDir, blockSize, axis, blockPosition);
							}
							continue;
						}

						normal.x = nXf ? 0 : dX;
						normal.y = nYf ? 0 : dY;
						normal.z = nZf ? 0 : dZ;

						if (neighbourCount == 2)
						{
							if (crossNeighbour2Count == 1) // SIDE
							{
								Vector3Int crossEdgePosition = voxelIndex + inVoxelDir - normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgePosition);
								if (crossEdgeFilled) // (NOT IN "EDGE TO EDGE" Cross)
								{
									axis = normal.ToAxis();
									yield return new Block(BlockType.SidePositive, inVoxelDir, blockSize, axis, blockPosition);
								}
							}
							else if (crossNeighbour2Count == 0) // SIDE TO EDGE NEGATIVE
							{
								axis = normal.ToAxis();

								//Vector3Int crossEdgeIndex = voxelIndex - normal;
								// bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgeIndex);
								// if (crossEdgeFilled)

								yield return new Block(BlockType.SideToNegativeEdge, inVoxelDir, blockSize, axis, blockPosition);
							}
						}
						else if (neighbourCount == 1 && crossNeighbourCount == 0) // EDGE
						{
							axis = Negate(normal);
							yield return new Block(BlockType.EdgePositive, inVoxelDir, blockSize, axis, blockPosition);
							continue;
						}
						if (neighbourCount == 1 && crossNeighbourCount == 1 && (!blockSetup.mergeCloseEdges || !nXYZf))  // EDGE
						{
							axis = Negate(normal);
							Vector3Int crossEdgePosition = voxelIndex + normal;
							bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgePosition);
							if (crossEdgeFilled)
							{
								yield return new Block(BlockType.EdgePositive, inVoxelDir, blockSize, axis, blockPosition);
								continue;
							}
						}
						if (neighbourCount == 1 && crossNeighbourCount == 1 && nXYZf) // EDGE TO EDGE
						{
							bool middleNeighbourFilled = voxelMap.IsFilledSafe(voxelIndex + normal);
							if (!middleNeighbourFilled)
							{
								axis = Negate(normal);
								SeparateVector(normal, out Vector3Int normal1, out Vector3Int normal2);
								Vector3Int crossEdgeIndex1 = voxelIndex + inVoxelDir - normal + normal1;
								bool crossEdgeFilled1 = voxelMap.IsFilledSafe(crossEdgeIndex1);
								Axis3D connectingAxis = crossEdgeFilled1 ? normal2.ToAxis() : normal1.ToAxis();
								if (axis == Axis3D.X && connectingAxis != Axis3D.Y)
									continue;
								if (axis == Axis3D.Y && connectingAxis != Axis3D.Z)
									continue;
								if (axis == Axis3D.Z && connectingAxis != Axis3D.X)
									continue;
								yield return new Block(BlockType.EdgeToEdge, inVoxelDir, blockSize, axis, blockPosition);
							}
						}
						else if (neighbourCount == 1 && crossNeighbourCount == 2 && nXYZf) // SIDE TO EDGE
						{
							axis = Negate(normal);
							Vector3Int crossEdgeIndex = voxelIndex + normal;
							bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgeIndex);
							if (!crossEdgeFilled)
							{
								yield return new Block(BlockType.SideToPositiveEdge, inVoxelDir, blockSize, axis,
									blockPosition);
							}
						}

						if (neighbourCount == 3)
						{
							if (crossNeighbourCount == 3) // CORNER NEGATIVE
							{
								yield return new Block(BlockType.CornerNegative, -inVoxelDir, blockSize, blockPosition + ((Vector3)inVoxelDir) * 0.5f);

								if (crossNeighbourCount != 3 && !blockSetup.mergeCloseEdges) // BREAKING POINT IN MODEL
								{
									axis = !nXYf ? Axis3D.Z : !nYZf ? Axis3D.X : Axis3D.Y;
									Vector3 normalVector = axis.ToVector().MultiplyAllAxis(inVoxelDir);
									blockPosition += (Vector3)inVoxelDir * 0.25f - normalVector * 0.5f;
									yield return new Block(BlockType.BreakPoint, inVoxelDir, blockSize, axis, blockPosition);
								}

								continue;
							}

							if (crossNeighbourCount == 0) // CROSS
							{
								yield return new Block(BlockType.CrossCorner, inVoxelDir, blockSize, blockPosition);
								continue;
							}

							normal.x = !nXf ? 0 : -dX;
							normal.y = !nYf ? 0 : -dY;
							normal.z = !nZf ? 0 : -dZ;


							if (crossNeighbourCount == 2 && !nXYZf) // EDGE NEGATIVE
							{
								axis = !nXYf ? Axis3D.Z : !nYZf ? Axis3D.X : Axis3D.Y;
								yield return new Block(BlockType.EdgeNegative, -inVoxelDir, blockSize, axis, blockPosition + ((Vector3)inVoxelDir) * 0.5f);
							}

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
						}
					}
		}

		static bool GetNeighbour(
			VoxelMap voxelMap, int voxelValue, int xi, int yi, int zi, bool voxelExists)
		{
			if (!voxelExists)
			{
				if (blockSetup.openOnSides) return true;
				else return false;
			}

			int val = voxelMap.GetVoxel(xi, yi, zi);
			return blockSetup.separateVoxelMaterials ?
				val == voxelValue : val.IsFilled();
		}

		static bool GetCrossNeighbour(
	VoxelMap voxelMap, int voxelValue, int xi, int yi, int zi, bool voxelExists)
		{
			if (!voxelExists)
			{
				if (blockSetup.openOnSides)
				{
					Vector3Int size = voxelMap.FullSize;
					xi = Mathf.Clamp(xi, 0, size.x - 1);
					yi = Mathf.Clamp(yi, 0, size.y - 1);
					zi = Mathf.Clamp(zi, 0, size.z - 1);
					return voxelMap.GetVoxel(xi, yi, zi).IsFilled();
				}
				else return false;
			}

			int val = voxelMap.GetVoxel(xi, yi, zi);
			return blockSetup.separateVoxelMaterials ?
				val == voxelValue : val.IsFilled();
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