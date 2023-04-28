using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{
	[Serializable]
	class BlockColorDictionary : SerializableDictionary<BlockType, Color>
	{
	}

	[Serializable]
	class BlockDrawingSettings
	{
		public bool drawVoxels = true;
		public Color voxelGizmoColor = new(1, 1, 1, 0.25f);
		public bool drawBlocks = true;
		public BlockColorDictionary blockColors = new();
		[Range(0, 0.25f)] public float margin = 0.1f;
	}




	[CreateAssetMenu(fileName = "BlockVoxelBuilder", menuName = "VoxelSystem/BlockVoxelBuilder", order = 4)]
	public class BlockVoxelBuilder : VoxelBuilder
	{
		[SerializeField] BlockDrawingSettings drawingSettings = new();
		[SerializeField] List<BlockLibrary> blockLibraries;

		[SerializeField] bool mergeCloseEdges = true;
		[SerializeField] int randomSeed = 0;

		static readonly List<Block> _blocks = new();

		protected override void BuildMesh(ArrayVoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals,
			List<Vector2> uv, List<int> triangles)
		{
			CalculateBlocks(voxelMap, _blocks, mergeCloseEdges);

			int selected = VoxelEditorWindow.SelectedPaletteIndex;
			selected = Mathf.Clamp(selected, 0, blockLibraries.Count - 1);
			BlockLibrary blockLibrary = blockLibraries[selected];

			BuildMeshFromBlocks(blockLibrary, _blocks, vertices, normals, uv, triangles);
		}

		protected override void BuildMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals,
			List<Vector2> uv, List<int> triangles)
		{
			CalculateBlocks(voxelMap, _blocks, mergeCloseEdges);

			int selected = VoxelEditorWindow.SelectedPaletteIndex;
			selected = Mathf.Clamp(selected, 0, blockLibraries.Count - 1);
			BlockLibrary blockLibrary = blockLibraries[selected];

			BuildMeshFromBlocks(blockLibrary, _blocks, vertices, normals, uv, triangles);
		}


	internal static void BuildMeshFromBlocks(IBlockLibrary blockLibrary, List<Block> blocksList,
			List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
		{
			BenchmarkTimer timer = new("Mesh Building");
			foreach (Block block in blocksList)
			{
				timer.StartModule("Search Mesh");
				if (!blockLibrary.TryGetMesh(block, out CustomMesh mesh, timer))
					continue;
				Vector3 offset = block.Center;
				timer.StartModule("Add Vertices");
				vertices.AddRange(mesh.vertices.Select(v => v + offset));
				normals.AddRange(mesh.normals);
				uv.AddRange(mesh.uv);
				timer.StartModule("Add Triangles");
				triangles.AddRange(mesh.triangles.Select(t => t + vertices.Count - mesh.vertices.Length));
			}
			timer.Stop();
			// Debug.Log(timer);
		}

		internal static void CalculateBlocks(VoxelMap voxelMap, List<Block> blocks, bool mergeCloseEdges)
		{
			blocks.Clear();
			Vector3Int size = voxelMap.FullSize;

			for (int x = size.x - 1; x >= 0; x--)
				for (int y = size.y - 1; y >= 0; y--)
					for (int z = size.z - 1; z >= 0; z--)
						blocks.AddRange(VoxelToBlocks(voxelMap, x, y, z, mergeCloseEdges));

			// Debug.Log($"Block Count: {blocks.Count}");
		}
		internal static void CalculateBlocks(OctVoxelMap voxelMap, List<Block> blocks, bool mergeCloseEdges)
		{
			blocks.Clear();

			for (int x = voxelMap.CanvasSize.x - 1; x >= 0; x--)
				for (int y = voxelMap.CanvasSize.y - 1; y >= 0; y--)
					for (int z = voxelMap.CanvasSize.z - 1; z >= 0; z--)
						blocks.AddRange(VoxelToBlocks(voxelMap, x, y, z, mergeCloseEdges));

			// Debug.Log($"Block Count: {blocks.Count}");
		}


		public override IEnumerable<PaletteItem> GetPaletteItems()
		{
			for (int index = 0; index < blockLibraries.Count; index++)
			{
				BlockLibrary blockLibrary = blockLibraries[index];
				yield return new PaletteItem
				{ value = index, name = blockLibrary.name, color = blockLibrary.LibraryColor };
			}
		}

		public override int PaletteLength => blockLibraries.Count;


		// Methods
		static System.Random _gizmoRandom;
		public override void DrawGizmos(ArrayVoxelMap map)
		{
			// Draw whole voxel map
			if (drawingSettings.drawVoxels)
			{
				Gizmos.color = drawingSettings.voxelGizmoColor;
				Vector3Int size = map.FullSize;
				for (int x = size.x - 1; x >= 0; x--)
					for (int y = size.y - 1; y >= 0; y--)
						for (int z = size.z - 1; z >= 0; z--)
						{
							if (map.GetVoxel(x, y, z).IsFilled())
								Gizmos.DrawWireCube(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), Vector3.one);
						}
			}

			// Draw Blocks
			if (drawingSettings.drawBlocks)
			{
				if (_blocks.IsEmpty())
				{
					CalculateBlocks(map, _blocks, false);
				}
				_gizmoRandom = new System.Random(randomSeed);
				foreach (Block block in _blocks)
				{
					Gizmos.color = drawingSettings.blockColors.TryGetValue(block.blockType, out Color color)
						? color
						: Color.magenta;
					block.DrawGizmo(drawingSettings.margin, _gizmoRandom);
				}
			}
		}

		static IEnumerable<Block> VoxelToBlocks(ArrayVoxelMap voxelMap, int vXi, int vYi, int vZi, bool mergeCloseEdges) // Voxel Index
		{
			int voxel = voxelMap.GetVoxel(vXi, vYi, vZi);
			bool isFilled = voxel.IsFilled();
			var voxelIndex = new Vector3Int(vXi, vYi, vZi);

			Vector3Int blockSize = Vector3Int.one;
			Vector3Int mapSize = voxelMap.FullSize;

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

						bool nXe = nXi >= 0 && nXi < mapSize.x; // Do Neighbour Row Exist
						bool nYe = nYi >= 0 && nYi < mapSize.y;
						bool nZe = nZi >= 0 && nZi < mapSize.z;

						bool nXf = nXe && voxelMap.GetVoxel(nXi, vYi, vZi).IsFilled(); // Is Neighbour Filled
						bool nYf = nYe && voxelMap.GetVoxel(vXi, nYi, vZi).IsFilled();
						bool nZf = nZe && voxelMap.GetVoxel(vXi, vYi, nZi).IsFilled();

						bool nXYf = nXe && nYe && voxelMap.GetVoxel(nXi, nYi, vZi).IsFilled(); // Is Cross Neighbour Filled
						bool nYZf = nYe && nZe && voxelMap.GetVoxel(vXi, nYi, nZi).IsFilled();
						bool nZXf = nZe && nXe && voxelMap.GetVoxel(nXi, vYi, nZi).IsFilled();

						bool nXYZf = nZe && nXe && nYe && voxelMap.GetVoxel(nXi, nYi, nZi).IsFilled(); // Is Corner Neighbour Filled

						// ---------------------------------------------------------------------------------------------

						int neighbourCount = (nXf ? 1 : 0) + (nYf ? 1 : 0) + (nZf ? 1 : 0);
						int crossNeighbourCount = (nXYf ? 1 : 0) + (nYZf ? 1 : 0) + (nZXf ? 1 : 0);
						int crossNeighbour2Count = (nXYf ? 1 : 0) + (nYZf ? 1 : 0) + (nZXf ? 1 : 0) + (nXYZf ? 1 : 0);

						var normal = new Vector3Int();

						if (isFilled)
						{
							if (neighbourCount == 0) // CORNER
							{
								yield return new Block(BlockType.CornerPositive, inVoxelDir, blockSize, blockPosition);

								if (crossNeighbourCount != 0 && mergeCloseEdges) // BREAK
								{
									Axis3D axis = nXYf ? Axis3D.Z : nYZf ? Axis3D.X : Axis3D.Y;
									Vector3 normalVector = axis.ToVector().MultiplyAllAxis(inVoxelDir);
									blockPosition += (Vector3)inVoxelDir * 0.25f - normalVector * 0.5f;
									yield return new Block(BlockType.BreakPoint, inVoxelDir, blockSize, axis, blockPosition);
								}
								continue;
							}

							if (neighbourCount == 3 && crossNeighbourCount == 0) // CROSS
							{
								yield return new Block(BlockType.CrossCorner, inVoxelDir, blockSize, blockPosition);
								continue;
							}

							normal.x = nXf ? 0 : dX;
							normal.y = nYf ? 0 : dY;
							normal.z = nZf ? 0 : dZ;

							if (neighbourCount == 2 && crossNeighbour2Count == 1) // SIDE
							{
								Vector3Int crossEdgePosition = voxelIndex + inVoxelDir - normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgePosition);
								if (crossEdgeFilled) // (NOT IN "EDGE TO EDGE" Cross)
								{
									Axis3D axis = normal.ToAxis();
									yield return new Block(BlockType.SidePositive, inVoxelDir, blockSize, axis, blockPosition);
								}
							}
							else if (neighbourCount == 1 && crossNeighbourCount == 0) // EDGE
							{
								Axis3D axis = Negate(normal);
								yield return new Block(BlockType.EdgePositive, inVoxelDir, blockSize, axis, blockPosition);
								continue;
							}
							if (neighbourCount == 1 && crossNeighbourCount == 1 && (!mergeCloseEdges || !nXYZf))  // EDGE
							{
								Axis3D axis = Negate(normal);
								Vector3Int crossEdgePosition = voxelIndex + normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgePosition);
								if (crossEdgeFilled)
								{
									yield return new Block(BlockType.EdgePositive, inVoxelDir, blockSize, axis, blockPosition);
									continue;
								}
							}
							if (neighbourCount == 1 && crossNeighbourCount == 1 && nXYZf)// EDGE TO EDGE
							{
								bool middleNeighbourFilled = voxelMap.IsFilledSafe(voxelIndex + normal);
								if (!middleNeighbourFilled)
								{
									Axis3D axis = Negate(normal);
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
								Axis3D axis = Negate(normal);
								Vector3Int crossEdgeIndex = voxelIndex + normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgeIndex);
								if (!crossEdgeFilled)
								{
									yield return new Block(BlockType.SideToPositiveEdge, inVoxelDir, blockSize, axis,
										blockPosition);
								}
							}
						}
						else
						{
							if (neighbourCount == 3) // CORNER NEGATIVE
							{
								yield return new Block(BlockType.CornerNegative, inVoxelDir, blockSize, blockPosition);

								if (crossNeighbourCount != 3 && !mergeCloseEdges) // BREAK
								{
									Axis3D axis = !nXYf ? Axis3D.Z : !nYZf ? Axis3D.X : Axis3D.Y;
									Vector3 normalVector = axis.ToVector().MultiplyAllAxis(inVoxelDir);
									blockPosition += (Vector3)inVoxelDir * 0.25f - normalVector * 0.5f;
									yield return new Block(BlockType.BreakPoint, inVoxelDir, blockSize, axis, blockPosition);
								}

								continue;
							}

							normal.x = !nXf ? 0 : -dX;
							normal.y = !nYf ? 0 : -dY;
							normal.z = !nZf ? 0 : -dZ;

							if (neighbourCount == 2 && crossNeighbourCount == 3) // EDGE NEGATIVE
							{
								Axis3D axis = Negate(normal);

								yield return new Block(BlockType.EdgeNegative, inVoxelDir, blockSize, axis, blockPosition);
							}
							if (neighbourCount == 2 && crossNeighbourCount == 2 && (mergeCloseEdges || nXYZf))   // EDGE NEGATIVE
							{
								Axis3D axis = Negate(normal);
								Vector3Int crossEdgePosition = voxelIndex - normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgePosition);
								if (!crossEdgeFilled)
								{
									yield return new Block(BlockType.EdgeNegative, inVoxelDir, blockSize, axis, blockPosition);
								}
							}
							else if (neighbourCount == 2 && crossNeighbourCount == 1 && !nXYZf) // SIDE TO EDGE NEGATIVE
							{
								Axis3D axis = Negate(normal);

								Vector3Int crossEdgeIndex = voxelIndex - normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgeIndex);
								if (crossEdgeFilled)
									yield return new Block(BlockType.SideToNegativeEdge, inVoxelDir, blockSize, axis,
										blockPosition);
							}
						}
					}
		}


		static IEnumerable<Block> VoxelToBlocks(VoxelMap voxelMap, int vXi, int vYi, int vZi, bool mergeCloseEdges) // Voxel Index
		{
			int voxel = voxelMap.GetVoxel(vXi, vYi, vZi);
			bool isFilled = voxel.IsFilled();
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

						bool nXf = nXe && voxelMap.GetVoxel(nXi, vYi, vZi).IsFilled(); // Is Neighbour Filled
						bool nYf = nYe && voxelMap.GetVoxel(vXi, nYi, vZi).IsFilled();
						bool nZf = nZe && voxelMap.GetVoxel(vXi, vYi, nZi).IsFilled();

						bool nXYf = nXe && nYe && voxelMap.GetVoxel(nXi, nYi, vZi).IsFilled(); // Is Cross Neighbour Filled
						bool nYZf = nYe && nZe && voxelMap.GetVoxel(vXi, nYi, nZi).IsFilled();
						bool nZXf = nZe && nXe && voxelMap.GetVoxel(nXi, vYi, nZi).IsFilled();

						bool nXYZf = nZe && nXe && nYe && voxelMap.GetVoxel(nXi, nYi, nZi).IsFilled(); // Is Corner Neighbour Filled

						// ---------------------------------------------------------------------------------------------

						int neighbourCount = (nXf ? 1 : 0) + (nYf ? 1 : 0) + (nZf ? 1 : 0);
						int crossNeighbourCount = (nXYf ? 1 : 0) + (nYZf ? 1 : 0) + (nZXf ? 1 : 0);
						int crossNeighbour2Count = (nXYf ? 1 : 0) + (nYZf ? 1 : 0) + (nZXf ? 1 : 0) + (nXYZf ? 1 : 0);

						var normal = new Vector3Int();

						if (isFilled)
						{
							if (neighbourCount == 0) // CORNER
							{
								yield return new Block(BlockType.CornerPositive, inVoxelDir, blockSize, blockPosition);

								if (crossNeighbourCount != 0 && mergeCloseEdges) // BREAK
								{
									Axis3D axis = nXYf ? Axis3D.Z : nYZf ? Axis3D.X : Axis3D.Y;
									Vector3 normalVector = axis.ToVector().MultiplyAllAxis(inVoxelDir);
									blockPosition += (Vector3)inVoxelDir * 0.25f - normalVector * 0.5f;
									yield return new Block(BlockType.BreakPoint, inVoxelDir, blockSize, axis, blockPosition);
								}
								continue;
							}

							if (neighbourCount == 3 && crossNeighbourCount == 0) // CROSS
							{
								yield return new Block(BlockType.CrossCorner, inVoxelDir, blockSize, blockPosition);
								continue;
							}

							normal.x = nXf ? 0 : dX;
							normal.y = nYf ? 0 : dY;
							normal.z = nZf ? 0 : dZ;

							if (neighbourCount == 2 && crossNeighbour2Count == 1) // SIDE
							{
								Vector3Int crossEdgePosition = voxelIndex + inVoxelDir - normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgePosition);
								if (crossEdgeFilled) // (NOT IN "EDGE TO EDGE" Cross)
								{
									Axis3D axis = normal.ToAxis();
									yield return new Block(BlockType.SidePositive, inVoxelDir, blockSize, axis, blockPosition);
								}
							}
							else if (neighbourCount == 1 && crossNeighbourCount == 0) // EDGE
							{
								Axis3D axis = Negate(normal);
								yield return new Block(BlockType.EdgePositive, inVoxelDir, blockSize, axis, blockPosition);
								continue;
							}
							if (neighbourCount == 1 && crossNeighbourCount == 1 && (!mergeCloseEdges || !nXYZf))  // EDGE
							{
								Axis3D axis = Negate(normal);
								Vector3Int crossEdgePosition = voxelIndex + normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgePosition);
								if (crossEdgeFilled)
								{
									yield return new Block(BlockType.EdgePositive, inVoxelDir, blockSize, axis, blockPosition);
									continue;
								}
							}
							if (neighbourCount == 1 && crossNeighbourCount == 1 && nXYZf)// EDGE TO EDGE
							{
								bool middleNeighbourFilled = voxelMap.IsFilledSafe(voxelIndex + normal);
								if (!middleNeighbourFilled)
								{
									Axis3D axis = Negate(normal);
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
								Axis3D axis = Negate(normal);
								Vector3Int crossEdgeIndex = voxelIndex + normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgeIndex);
								if (!crossEdgeFilled)
								{
									yield return new Block(BlockType.SideToPositiveEdge, inVoxelDir, blockSize, axis,
										blockPosition);
								}
							}
						}
						else
						{
							if (neighbourCount == 3) // CORNER NEGATIVE
							{
								yield return new Block(BlockType.CornerNegative, inVoxelDir, blockSize, blockPosition);

								if (crossNeighbourCount != 3 && !mergeCloseEdges) // BREAK
								{
									Axis3D axis = !nXYf ? Axis3D.Z : !nYZf ? Axis3D.X : Axis3D.Y;
									Vector3 normalVector = axis.ToVector().MultiplyAllAxis(inVoxelDir);
									blockPosition += (Vector3)inVoxelDir * 0.25f - normalVector * 0.5f;
									yield return new Block(BlockType.BreakPoint, inVoxelDir, blockSize, axis, blockPosition);
								}

								continue;
							}

							normal.x = !nXf ? 0 : -dX;
							normal.y = !nYf ? 0 : -dY;
							normal.z = !nZf ? 0 : -dZ;

							if (neighbourCount == 2 && crossNeighbourCount == 3) // EDGE NEGATIVE
							{
								Axis3D axis = Negate(normal);

								yield return new Block(BlockType.EdgeNegative, inVoxelDir, blockSize, axis, blockPosition);
							}
							if (neighbourCount == 2 && crossNeighbourCount == 2 && (mergeCloseEdges || nXYZf))   // EDGE NEGATIVE
							{
								Axis3D axis = Negate(normal);
								Vector3Int crossEdgePosition = voxelIndex - normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgePosition);
								if (!crossEdgeFilled)
								{
									yield return new Block(BlockType.EdgeNegative, inVoxelDir, blockSize, axis, blockPosition);
								}
							}
							else if (neighbourCount == 2 && crossNeighbourCount == 1 && !nXYZf) // SIDE TO EDGE NEGATIVE
							{
								Axis3D axis = Negate(normal);

								Vector3Int crossEdgeIndex = voxelIndex - normal;
								bool crossEdgeFilled = voxelMap.IsFilledSafe(crossEdgeIndex);
								if (crossEdgeFilled)
									yield return new Block(BlockType.SideToNegativeEdge, inVoxelDir, blockSize, axis,
										blockPosition);
							}
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