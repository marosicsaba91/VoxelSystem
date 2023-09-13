using MUtility;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	enum VoxelConnectionType
	{
		CloseFully,
		Continue,
		CloseEdge
	}

	[CreateAssetMenu(fileName = "Block Voxel", menuName = "Voxel System/Block Voxel")]
	public class BlockVoxel : UniversalVoxelBuilder
	{
		[SerializeField] bool mergeCloseEdges;
		[SerializeField] VoxelConnectionType connectionOnMapEdge = VoxelConnectionType.CloseFully;
		[SerializeField] bool drawBetweenVoxelChange = false;

		[SerializeField] VoxelBlockLibrary blockLibrary;

		readonly Dictionary<Vector3Int, Block> blocks = new();

		static Dictionary<Vector3Int, Block> _block = new();
		static readonly NeighbourType[,,] neighbours = new NeighbourType[3, 3, 3];

		static VoxelMap _currentVoxelMap;

		static readonly List<Vector3> _breakPoints = new();


		protected override void BeforeMeshGeneration(VoxelMap map, VoxelPalette palette, int voxelTypeIndex) { }

		protected override void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int voxelTypeIndex,
			List<Vector3> vertices,
			List<Vector3> normals,
			List<Vector2> uv,
			List<int> triangles)
		{
			CalculateBlocks(blocks, voxelPositions, map);
			Vector3 quarter = Vector3.one * 0.25f;

			foreach (KeyValuePair<Vector3Int, Block> blockWithPosition in blocks)
			{
				Vector3Int subVoxelIndex = blockWithPosition.Key;
				Block block = blockWithPosition.Value;
				if (!blockLibrary.TryGetMesh(block.blockType, block.axis, block.subVoxel, out CustomMesh mesh))
					continue;
				Vector3 offset = block.Center(subVoxelIndex);

				// OPTIMALIZÁLHATÓ:
				vertices.AddRange(mesh.vertices.Select(v => v + offset));
				normals.AddRange(mesh.normals);
				uv.AddRange(mesh.uv);
				triangles.AddRange(mesh.triangles.Select(t => t + vertices.Count - mesh.vertices.Length));
			}
		}


		public void CalculateBlocks(Dictionary<Vector3Int, Block> blocks, List<Vector3Int> indexes, VoxelMap map)
		{
			blocks.Clear();
			_block.Clear();
			_breakPoints.Clear();

			_currentVoxelMap = map;

			for (int i = 0; i < indexes.Count; i++)
			{
				Vector3Int index = indexes[i];
				int voxelValue = map.GetVoxel(index);
				if (voxelValue.IsEmpty()) continue;

				for (int dX = -1; dX <= 1; dX += 1) // Sub-Voxels
					for (int dY = -1; dY <= 1; dY += 1)
						for (int dZ = -1; dZ <= 1; dZ += 1)
						{
							NeighbourType n = GetAnyNeighbour(voxelValue, index.x, index.y, index.z, dX, dY, dZ);
							neighbours[1 + dX, 1 + dY, 1 + dZ] = n;
						}


				_block = blocks;
				for (int dX = -1; dX <= 1; dX += 2) // Sub-Voxels
					for (int dY = -1; dY <= 1; dY += 2)
						for (int dZ = -1; dZ <= 1; dZ += 2)
							SubVoxelToBlock(index.x, index.y, index.z, dX, dY, dZ);
			}
		}


		NeighbourType GetAnyNeighbour(int currentValue, int x, int y, int z, int dX, int dY, int dZ)
		{
			Vector3Int size = _currentVoxelMap.FullSize;

			int neighbourValue;

			int nx = x + dX;
			int ny = y + dY;
			int nz = z + dZ;

			bool voxelExists =
				nx >= 0 && nx < size.x &&
				ny >= 0 && ny < size.y &&
				nz >= 0 && nz < size.z;

			if (voxelExists)
			{
				neighbourValue = _currentVoxelMap.GetVoxel(nx, ny, nz);
				if (neighbourValue == currentValue)
					return NeighbourType.SameFilled;

				if (neighbourValue.IsFilled())
					return NeighbourType.DifferentFilled;
				else
					return NeighbourType.EmptyInMap;


			}


			// Neighbour Do NOT Exists:
			if (connectionOnMapEdge != VoxelConnectionType.Continue)
				return NeighbourType.EmptyOutOfMap;

			int xin = Mathf.Clamp(x, 0, size.x - 1);
			int yin = Mathf.Clamp(y, 0, size.y - 1);
			int zin = Mathf.Clamp(z, 0, size.z - 1);
			neighbourValue = _currentVoxelMap.GetVoxel(xin, yin, zin);

			return
				neighbourValue == currentValue ? NeighbourType.SameFilled :
				neighbourValue.IsFilled() ? NeighbourType.DifferentFilled :
				NeighbourType.EmptyInMap;
		}

		void SubVoxelToBlock(int vXi, int vYi, int vZi, int dX, int dY, int dZ)
		{

			NeighbourType nX = neighbours[1 + dX, 1, 1];
			NeighbourType nY = neighbours[1, 1 + dY, 1];
			NeighbourType nZ = neighbours[1, 1, 1 + dZ];
			NeighbourType nXY = neighbours[1 + dX, 1 + dY, 1];
			NeighbourType nYZ = neighbours[1, 1 + dY, 1 + dZ];
			NeighbourType nZX = neighbours[1 + dX, 1, 1 + dZ];
			NeighbourType nXYZ = neighbours[1 + dX, 1 + dY, 1 + dZ];

			// ---------------------------------------------------------------------------------------------

			// Between Two Different Type
			if (!drawBetweenVoxelChange && nX.IsFilled() && nY.IsFilled() && nZ.IsFilled() && nXY.IsFilled() && nYZ.IsFilled() && nZX.IsFilled() && nXYZ.IsFilled())
				return;


			//On The Edge of the Map
			if (connectionOnMapEdge == VoxelConnectionType.CloseEdge &&
				nX.SameOrOut() && nY.SameOrOut() && nZ.SameOrOut() && nXY.SameOrOut() && nYZ.SameOrOut() && nZX.SameOrOut() && nXYZ.SameOrOut())
				return;

			// ---------------------------------------------------------------------------------------------

			int neighbourCount = (nX.IsSame() ? 1 : 0) + (nY.IsSame() ? 1 : 0) + (nZ.IsSame() ? 1 : 0);
			int crossNeighbourCount = (nXY.IsSame() ? 1 : 0) + (nYZ.IsSame() ? 1 : 0) + (nZX.IsSame() ? 1 : 0);
			int crossNeighbour2Count = (nXY.IsSame() ? 1 : 0) + (nYZ.IsSame() ? 1 : 0) + (nZX.IsSame() ? 1 : 0) + (nXYZ.IsSame() ? 1 : 0);


			// ---------------------------------------------------------------------------------------------

			Vector3Int voxelIndex = new(vXi, vYi, vZi);
			Vector3Int subVoxel = new(dX, dY, dZ);
			Vector3Int subVoxelIndex = (voxelIndex * 2) + (subVoxel + Vector3Int.one) / 2;

			// Is Filled On The Edge ---------------------------------------------------------------------------------------------

			if (neighbourCount == 0) // CORNER // ----------------------------------------------------------------------------
			{
				_block.TryAdd(subVoxelIndex, new Block(BlockType.CornerPositive, subVoxel));

				if (crossNeighbourCount != 0 && mergeCloseEdges) // BREAKING POINT IN MODEL
				{
					Axis3D axis = nXY.IsSame() ? Axis3D.Z : nYZ.IsSame() ? Axis3D.X : Axis3D.Y;
					Vector3 normalVector = axis.ToVector().MultiplyAllAxis(subVoxel);
					_breakPoints.Add(voxelIndex + (Vector3)subVoxel * 0.25f - normalVector * 0.5f);
				}
				return;
			}

			if (neighbourCount == 3) // CROSS // ----------------------------------------------------------------------------
			{
				if (crossNeighbourCount == 0) // CROSS
				{
					_block.Add(subVoxelIndex, new Block(BlockType.CrossCorner, subVoxel));
				}
				return;
			}

			Vector3Int normal = new()
			{
				x = nX.IsSame() ? 0 : dX,
				y = nY.IsSame() ? 0 : dY,
				z = nZ.IsSame() ? 0 : dZ
			};

			if (neighbourCount == 2) // SIDE // -----------------------------------------------------------------------------------
			{

				if (crossNeighbourCount == 0) // SIDE TO EDGE NEGATIVE
				{
					Axis3D axis = normal.ToAxis();
					_block.Add(subVoxelIndex, new Block(BlockType.SideToNegativeEdge, subVoxel, axis));
					return;
				}

				Vector3Int inPlaneNeighbourIndex = Vector3Int.one + subVoxel - normal;
				NeighbourType inPlaneNeighbour = neighbours[inPlaneNeighbourIndex.x, inPlaneNeighbourIndex.y, inPlaneNeighbourIndex.z];

				if (crossNeighbour2Count == 1 && inPlaneNeighbour.IsSame()) // SIDE
				{
					Axis3D axis = normal.ToAxis();
					_block.Add(subVoxelIndex, new Block(BlockType.SidePositive, subVoxel, axis));
				}

				else if (crossNeighbourCount == 3 || (crossNeighbourCount == 2 && !inPlaneNeighbour.IsSame())) // NEGATIVE CORNERS
				{
					subVoxel -= normal * 2;
					_block.AddOrChangeValue(subVoxelIndex + normal, new Block(BlockType.CornerNegative, subVoxel));
					return;
				}

				bool negativeEdge =
					(crossNeighbourCount == 2 && nXYZ.IsSame()) || //  NEGATIVE EDGE (COMMON)
					(crossNeighbourCount == 1 && !inPlaneNeighbour.IsSame() && nXYZ.IsSame()); //  NEGATIVE EDGE (COLUMNS - Different Size)

				if (negativeEdge) // NEGATIVE EDGE
				{

					SeparateVector(subVoxel - normal, out Vector3Int normal1, out Vector3Int normal2);
					Vector3Int crossEdgeIndex = Vector3Int.one + normal + normal1;
					NeighbourType crossEdgeNeighbour = neighbours[crossEdgeIndex.x, crossEdgeIndex.y, crossEdgeIndex.z];
					subVoxel -= normal * 2;
					Axis3D axis = crossEdgeNeighbour.IsSame() ? normal2.ToAxis() : normal1.ToAxis();
					_block.TryAdd(subVoxelIndex + normal, new Block(BlockType.EdgeNegative, subVoxel, axis));
				}

				return;
			}
			else if (neighbourCount == 1) // EDGE //
			{
				Axis3D axis = Negate(normal);

				if (crossNeighbourCount == 0) // EDGE
				{
					_block.Add(subVoxelIndex, new Block(BlockType.EdgePositive, subVoxel, axis));
					return;
				}

				Vector3Int crossEdgeIndex = Vector3Int.one + normal;
				NeighbourType crossEdgeNeighbour = neighbours[crossEdgeIndex.x, crossEdgeIndex.y, crossEdgeIndex.z];


				bool negativeEdge =
					(crossEdgeNeighbour.IsSame() && mergeCloseEdges && nXYZ.IsSame()) ||  // NEGATIVE EDGE (COLUMNS - Merge)
					(crossEdgeNeighbour.IsSame() && crossNeighbourCount == 2 && nXYZ.IsSame());      // NEGATIVE EDGE (COLUMNS - Different Size)

				if (negativeEdge) // NEGATIVE EDGE
				{
					SeparateVector(normal, out Vector3Int normal1, out _);
					subVoxel -= normal1 * 2;
					_block.TryAdd(subVoxelIndex + normal1, new Block(BlockType.EdgeNegative, subVoxel, axis));
					return;
				}

				if (crossNeighbourCount == 1)
				{
					if (crossEdgeNeighbour.IsSame())
					{
						if (!mergeCloseEdges || !nXYZ.IsSame()) // EDGE						
							_block.Add(subVoxelIndex, new Block(BlockType.EdgePositive, subVoxel, axis));
					}
					else
					{
						if (nXYZ.IsSame()) // EDGE TO EDGE
						{
							SeparateVector(normal, out Vector3Int normal1, out Vector3Int normal2);
							crossEdgeIndex = Vector3Int.one + subVoxel - normal + normal1;
							crossEdgeNeighbour = neighbours[crossEdgeIndex.x, crossEdgeIndex.y, crossEdgeIndex.z];
							Axis3D connectingAxis = crossEdgeNeighbour.IsSame() ? normal2.ToAxis() : normal1.ToAxis();
							if (axis == Axis3D.X && connectingAxis != Axis3D.Y)
								return;
							if (axis == Axis3D.Y && connectingAxis != Axis3D.Z)
								return;
							if (axis == Axis3D.Z && connectingAxis != Axis3D.X)
								return;
							_block.Add(subVoxelIndex, new Block(BlockType.EdgeToEdge, subVoxel, axis));
						}
					}
					return;
				}


				if (crossNeighbourCount == 2 && nXYZ.IsSame() && !crossEdgeNeighbour.IsSame()) // EDGE TO SIDE
				{
					_block.Add(subVoxelIndex, new Block(BlockType.SideToPositiveEdge, subVoxel, axis));
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