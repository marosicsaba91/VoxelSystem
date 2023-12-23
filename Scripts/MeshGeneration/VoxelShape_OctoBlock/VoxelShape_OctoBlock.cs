using MeshUtility;
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

	[CreateAssetMenu(fileName = "OctoBlockVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: OctoBlock", order = EditorConstants.soOrder_VoxelShape)]

	public class VoxelShape_OctoBlock : VoxelShapeBuilder
	{
		[SerializeField] OctoBlockLibrary blockLibrary;

		[Header("Other SetupFromMesh")]
		[SerializeField] bool mergeCloseEdges;
		[SerializeField] VoxelConnectionType onMapEdge = VoxelConnectionType.CloseFully;
		[SerializeField] bool drawBetweenVoxelChange = false;
		[SerializeField] bool isTransparent = false;


		protected override bool IsInitialized => blockLibrary != null;

		readonly Dictionary<Vector3Int, OctoBlock> blocks = new();

		static Dictionary<Vector3Int, OctoBlock> _block = new();
		static readonly NeighbourType[,,] neighbours = new NeighbourType[3, 3, 3];

		static VoxelMap _currentVoxelMap;

		static readonly List<Vector3> _breakPoints = new();
		protected override void InitializeCachedData() { }

		protected sealed override void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder)
		{
			CalculateBlocks(blocks, voxelPositions, map);
			Vector3 quarter = Vector3.one * 0.25f;

			foreach (KeyValuePair<Vector3Int, OctoBlock> blockWithPosition in blocks)
			{
				Vector3Int subVoxelIndex = blockWithPosition.Key;
				OctoBlock block = blockWithPosition.Value;
				if (!blockLibrary.TryGetMesh(block.blockType, block.axis, block.subVoxel, out MeshBuilder mesh))
					continue;
				Vector3 offset = block.Center(subVoxelIndex);

				// OPTIMALIZÁLHATÓ:
				meshBuilder.vertices.AddRange(mesh.vertices.Select(v => v + offset));
				meshBuilder.normals.AddRange(mesh.normals);
				meshBuilder.uv.AddRange(mesh.uv);
				meshBuilder.triangles.AddRange(mesh.triangles.Select(t => t + meshBuilder.VertexCount - mesh.VertexCount));
			}
		}

		public void CalculateBlocks(Dictionary<Vector3Int, OctoBlock> blocks, List<Vector3Int> indexes, VoxelMap map)
		{
			blocks.Clear();
			_block.Clear();
			_breakPoints.Clear();

			_currentVoxelMap = map;

			for (int i = 0; i < indexes.Count; i++)
			{
				Vector3Int index = indexes[i];
				Voxel voxelValue = map.GetVoxel(index);
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

		NeighbourType GetAnyNeighbour(Voxel voxel, int x, int y, int z, int dX, int dY, int dZ)
		{
			Vector3Int voxelCoordinate = new(x, y, z);
			Vector3Int neighbourVector = new(dX, dY, dZ);
			Vector3Int neighbourCoordinate = voxelCoordinate + neighbourVector;

			if (_currentVoxelMap.TryGetVoxel(neighbourCoordinate, out Voxel neighbour))
			{
				if (neighbour.shapeId == voxel.shapeId && neighbour.materialIndex == voxel.materialIndex)
					return NeighbourType.SameFilled;

				GeneralDirection3D testedSide = DirectionUtility.GeneralDirection3DFromVector(neighbourVector);
				if (!neighbour.IsFilled(testedSide))
					return NeighbourType.EmptyInMap;
				 
				return NeighbourType.DifferentFilled; 
			}


			// Neighbour Do NOT Exists:
			if (onMapEdge != VoxelConnectionType.Continue)
				return NeighbourType.EmptyOutOfMap;

			Vector3Int clamped = _currentVoxelMap.ClampCoordinate(neighbourCoordinate);
			neighbour = _currentVoxelMap.GetVoxel(clamped);

			if (neighbour.shapeId == voxel.shapeId && neighbour.materialIndex == voxel.materialIndex)
				return NeighbourType.SameFilled;
			if (neighbour.IsFilled())
				return NeighbourType.DifferentFilled;
			else
				return NeighbourType.EmptyInMap;
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
			if (!drawBetweenVoxelChange &&
				nX.IsFilled() && nY.IsFilled() && nZ.IsFilled() &&
				nXY.IsFilled() && nYZ.IsFilled() && nZX.IsFilled() &&
				nXYZ.IsFilled())
				return;

			//On The Edge of the Map
			if (onMapEdge == VoxelConnectionType.CloseEdge &&
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
				_block.TryAdd(subVoxelIndex, new OctoBlock(OctoBlockType.CornerPositive, subVoxel));

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
					_block.Add(subVoxelIndex, new OctoBlock(OctoBlockType.CrossCorner, subVoxel));
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
					_block.Add(subVoxelIndex, new OctoBlock(OctoBlockType.SideToNegativeEdge, subVoxel, axis));
					return;
				}

				Vector3Int inPlaneNeighbourIndex = Vector3Int.one + subVoxel - normal;
				NeighbourType inPlaneNeighbour = neighbours[inPlaneNeighbourIndex.x, inPlaneNeighbourIndex.y, inPlaneNeighbourIndex.z];

				if (crossNeighbour2Count == 1 && inPlaneNeighbour.IsSame()) // SIDE
				{
					Axis3D axis = normal.ToAxis();
					_block.Add(subVoxelIndex, new OctoBlock(OctoBlockType.SidePositive, subVoxel, axis));
				}

				else if (crossNeighbourCount == 3 || (crossNeighbourCount == 2 && !inPlaneNeighbour.IsSame())) // NEGATIVE CORNERS
				{
					subVoxel -= normal * 2;
					_block.AddOrChangeValue(subVoxelIndex + normal, new OctoBlock(OctoBlockType.CornerNegative, subVoxel));
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
					_block.TryAdd(subVoxelIndex + normal, new OctoBlock(OctoBlockType.EdgeNegative, subVoxel, axis));
				}

				return;
			}
			else if (neighbourCount == 1) // EDGE //
			{
				Axis3D axis = Negate(normal);

				if (crossNeighbourCount == 0) // EDGE
				{
					OctoBlock newSubVoxel = new (OctoBlockType.EdgePositive, subVoxel, axis);
					if (_block.ContainsKey(subVoxelIndex)) 
					{
						_block[subVoxel] = newSubVoxel;
						Debug.LogWarning("Something ain't right! Subvoxel: " + subVoxel);
					}
					else
						_block.Add(subVoxelIndex, newSubVoxel);
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
					_block.TryAdd(subVoxelIndex + normal1, new OctoBlock(OctoBlockType.EdgeNegative, subVoxel, axis));
					return;
				}

				if (crossNeighbourCount == 1)
				{
					if (crossEdgeNeighbour.IsSame())
					{
						if (!mergeCloseEdges || !nXYZ.IsSame()) // EDGE						
							_block.Add(subVoxelIndex, new OctoBlock(OctoBlockType.EdgePositive, subVoxel, axis));
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
							_block.Add(subVoxelIndex, new OctoBlock(OctoBlockType.EdgeToEdge, subVoxel, axis));
						}
					}
					return;
				}


				if (crossNeighbourCount == 2 && nXYZ.IsSame() && !crossEdgeNeighbour.IsSame()) // EDGE TO SIDE
				{
					_block.Add(subVoxelIndex, new OctoBlock(OctoBlockType.SideToPositiveEdge, subVoxel, axis));
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
		static void SeparateVector(Vector3Int vector, out Vector3Int a, out Vector3Int b, out Vector3Int c)
		{
			a = new Vector3Int(vector.x, vector.y, 0);
			b = new Vector3Int(0, vector.y, vector.z);
			c = new Vector3Int(vector.x, 0, vector.z);
		}

		static Axis3D Negate(Vector3 v)
		{
			float x = v.x == 0 ? 1 : 0;
			float y = v.y == 0 ? 1 : 0;
			float z = v.z == 0 ? 1 : 0;
			return new Vector3(x, y, z).ToAxis();
		}

		protected override void SetupClosedSides(VoxelMap map, List<Vector3Int> voxelPositions)
		{
			bool close = !isTransparent;
			for (int i = 0; i < voxelPositions.Count; i++)
			{
				Vector3Int voxelPosition = voxelPositions[i];
				Voxel v = map.GetVoxel(voxelPosition);
				v.SetAllSideClose(close);
				map.SetVoxel(voxelPosition, v);
			}
		}
	}
}