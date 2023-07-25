using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public static class VoxelMap_Search
	{
		public static int _roundLimit = 1000;

		static readonly HashSet<Vector3Int> _alreadyChecked = new();
		static readonly HashSet<Vector3Int> _tempIndices1 = new();
		static readonly HashSet<Vector3Int> _tempIndices2 = new();
		static int _roundIndex = 0;
		static readonly List<Vector3Int> _searchDirections = new ();
		static bool _planeOnly;
		static Vector3Int _normal;
		public static void SearchChunk(this VoxelMap map, HashSet<Vector3Int> result, Vector3Int startIndex, bool sameColorOnly)
		{
			int searchValue = map.GetVoxel(startIndex);
			result.Clear();

			_alreadyChecked.Clear();
			_tempIndices1.Clear();
			_tempIndices2.Clear();
			_tempIndices1.Add(startIndex);
			_roundIndex = 0;
			_planeOnly = false;

			_searchDirections.Clear();
			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
				_searchDirections.Add(DirectionUtility.generalDirection3DValues[i].ToVectorInt());

			map.Search(result, searchValue, sameColorOnly);
		}
		public static void SearchPlane(this VoxelMap map, HashSet<Vector3Int> result, Vector3Int startIndex, GeneralDirection3D side, bool sameColorOnly)
		{
			int searchValue = map.GetVoxel(startIndex);
			result.Clear();

			_alreadyChecked.Clear();
			_tempIndices1.Clear();
			_tempIndices2.Clear();
			_tempIndices1.Add(startIndex);
			_roundIndex = 0;
			_normal = side.ToVectorInt();
			_planeOnly = true;


			Axis3D axis = side.GetAxis();
			Axis3D p1 = (Axis3D)(((int)axis + 1) % 3);
			Axis3D p2 = (Axis3D)(((int)axis + 2) % 3);

			GeneralDirection3D d1 = p1.ToPositiveDirection();
			GeneralDirection3D d2 = d1.Opposite();
			GeneralDirection3D d3 = p2.ToPositiveDirection();
			GeneralDirection3D d4 = d3.Opposite();

			_searchDirections.Clear();
			_searchDirections.Add(d1.ToVectorInt());
			_searchDirections.Add(d2.ToVectorInt());
			_searchDirections.Add(d3.ToVectorInt());
			_searchDirections.Add(d4.ToVectorInt());
			 
			Search(map, result, searchValue, sameColorOnly);
		}

		static void Search(this VoxelMap map, HashSet<Vector3Int> result, int searchValue, bool sameColorOnly)
		{
			HashSet<Vector3Int> current, next;
			do
			{
				current = _roundIndex % 2 == 0 ? _tempIndices1 : _tempIndices2;
				next = _roundIndex % 2 == 0 ? _tempIndices2 : _tempIndices1;

				next.Clear();

				foreach (Vector3Int index in current)
				{
					result.Add(index);
					_alreadyChecked.Add(index);

					for (int i = 0; i < _searchDirections.Count; i++)
					{
						Vector3Int direction = _searchDirections[i];
						Vector3Int nextIndex = direction + index;
						if (_alreadyChecked.Contains(nextIndex))
							continue;
						if (!map.IsValidCoord(nextIndex))
						{
							_alreadyChecked.Add(nextIndex);
							continue;
						}
						int nextVoxel = map.GetVoxel(nextIndex);
						bool isDifferent = sameColorOnly
							? nextVoxel != searchValue
							: nextVoxel.IsFilled() != searchValue.IsFilled();

						if (isDifferent)
						{
							_alreadyChecked.Add(nextIndex);
							continue;
						}

						if (_planeOnly)
						{
							Vector3Int upperIndex = nextIndex + _normal;
							if (map.IsFilledSafe(upperIndex))
							{
								_alreadyChecked.Add(nextIndex);
								continue;
							}
						}
						next.Add(nextIndex);
					}
				}

				_roundIndex++;
			} while (!next.IsEmpty() && _roundIndex < _roundLimit);
		}

		/*
		static void SearchChunk(this VoxelMap map, HashSet<Vector3Int> result, int searchValue, bool sameColorOnly)
		{
			HashSet<Vector3Int> current, next;
			do
			{
				current = _roundIndex % 2 == 0 ? _tempIndices1 : _tempIndices2;
				next = _roundIndex % 2 == 0 ? _tempIndices2 : _tempIndices1;

				next.Clear();
				GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;

				foreach (Vector3Int index in current)
				{
					result.Add(index);
					_alreadyChecked.Add(index);

					for (int i = 0; i < directions.Length; i++)
					{
						GeneralDirection3D direction = directions[i];
						Vector3Int nextIndex = direction.ToVectorInt() + index;
						if (_alreadyChecked.Contains(nextIndex))
							continue;
						if (!map.IsValidCoord(nextIndex))
						{
							_alreadyChecked.Add(nextIndex);
							continue;
						}
						int nextVoxel = map.GetVoxel(nextIndex);

						bool isDifferent = sameColorOnly
							? nextVoxel != searchValue
							: nextVoxel.IsFilled() != searchValue.IsFilled();

						if (isDifferent)
						{
							_alreadyChecked.Add(nextIndex);
							continue;
						}

						next.Add(nextIndex);
					}
				}

				_roundIndex++;
			} while (!next.IsEmpty());
		}
		*/
	}
}
