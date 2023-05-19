using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_Face : VoxelToolHandler
	{
		public sealed override VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => allVoxelActions;
		protected sealed override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = false;
			return true;
		}

		HashSet<Vector3Int> originalSide = new HashSet<Vector3Int>();
		HashSet<Vector3Int> alreadyChecked = new HashSet<Vector3Int>();
		Vector3Int[] searchDirections = new Vector3Int[4];
		Vector3Int normal;
		 
		static HashSet<Vector3Int> _nextRound1 = new HashSet<Vector3Int>();
		static HashSet<Vector3Int> _nextRound2 = new HashSet<Vector3Int>();
		int _checkRoundIndex = 0;

		protected sealed override void OnDrawCursor(IVoxelEditor voxelEditor, Color actionColor, VoxelHit hit)
		{
			Drawable side = GetDrawableVoxelSide(_lastValidHit);
			Draw(side, actionColor);
			
			SetupSearchDirections(hit.side);
			SearchPlane(voxelEditor.Map, hit.voxelIndex, voxelEditor.SelectedAction == VoxelAction.Repaint);

			foreach (Vector3Int item in originalSide)
			{
				Drawable d = new(GetVoxelSide(item, hit.side, 1f)); 
				Draw(d, actionColor);
			}
			
		}
		 
		void SetupSearchDirections(GeneralDirection3D side)
		{
			Axis3D axis = side.GetAxis();
			normal = side.ToVectorInt();

			var p1 = (Axis3D)(((int)axis + 1) % 3);
			var p2 = (Axis3D)(((int)axis + 2) % 3);

			GeneralDirection3D d1 = p1.ToPositiveDirection();
			GeneralDirection3D d2 = d1.Opposite();
			GeneralDirection3D d3 = p2.ToPositiveDirection();
			GeneralDirection3D d4 = d3.Opposite();

			searchDirections[0] = d1.ToVectorInt();
			searchDirections[1] = d2.ToVectorInt();
			searchDirections[2] = d3.ToVectorInt();
			searchDirections[3] = d4.ToVectorInt();
		}

		public void SearchPlane(VoxelMap map, Vector3Int startIndex, bool colorOnly)
		{ 
			int startVoxel = map.GetVoxel(startIndex);

			if (startVoxel.IsEmpty())
			{
				originalSide.Clear();
				return;
			}

			originalSide.Clear();
			alreadyChecked.Clear();
			_nextRound1.Clear();
			_nextRound2.Clear();
			_nextRound1.Add(startIndex);
			_checkRoundIndex = 0;

			SearchPlane(map, startVoxel, colorOnly); 
		}

		void SearchPlane(VoxelMap map, int searchValue, bool colorOnly)
		{
			HashSet<Vector3Int> current, next;
			do
			{
				current = _checkRoundIndex % 2 == 0 ? _nextRound1 : _nextRound2;
				next = _checkRoundIndex % 2 == 0 ? _nextRound2 : _nextRound1;

				next.Clear(); 

				foreach (Vector3Int index in current)
				{
					originalSide.Add(index);
					alreadyChecked.Add(index);

					for (int i = 0; i < searchDirections.Length; i++)
					{
						Vector3Int direction = searchDirections[i];
						Vector3Int nextIndex = direction + index;
						if (alreadyChecked.Contains(nextIndex))
							continue;
						if (!map.IsValidCoord(nextIndex))
						{
							alreadyChecked.Add(nextIndex);
							continue;
						}
						int nextVoxel = map.GetVoxel(nextIndex);
						bool isDifferent = colorOnly
							? nextVoxel != searchValue
							: nextVoxel.IsFilled() != searchValue.IsFilled();

						if (isDifferent)
						{
							alreadyChecked.Add(nextIndex);
							continue;
						}

						Vector3Int upperIndex = nextIndex + normal;
						if (map.IsFilledSafe(upperIndex))
						{
							alreadyChecked.Add(nextIndex);
							continue;
						}


						next.Add(nextIndex);
					}
				}

				_checkRoundIndex++;
			} while (!next.IsEmpty());
		}
	}
}
