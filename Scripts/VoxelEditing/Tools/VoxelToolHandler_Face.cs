using MUtility;
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

		readonly HashSet<Vector3Int> _originalSide = new(FastVector3IntComparer.instance);

		GeneralDirection3D surfaceDirection;
		Vector3Int surfaceNormal;
		int offsetValue;
		int lastOffsetValue;

		protected sealed override void OnDrawCursor(IVoxelEditor voxelEditor, Color actionColor, VoxelHit hit)
		{
			if (voxelEditor.ToolState == ToolState.None)
			{
				offsetValue = 0;
				lastOffsetValue = 0;
				WireShape side = GetDrawableVoxelSide(lastValidHit);
				Draw(side, actionColor);

				voxelEditor.Map.SearchPlane(_originalSide, hit.voxelIndex, hit.side, voxelEditor.SelectedAction.GetEqualityTestFunction());
			}

			WireShape d = VoxelMap_DrawingUtilities.GetContourDrawable(_originalSide);
			if (offsetValue > 0 ^ !surfaceDirection.IsPositive())
			{
				Vector3 off = mouseDownHit.voxelIndex.MultiplyAllAxis(surfaceNormal.Abs());
				d.Translate(-off);
				d.Scale(Vector3Int.one + (offsetValue * surfaceNormal));
				d.Translate(off);
			}
			else if (offsetValue > 0 && !surfaceDirection.IsPositive())
			{
				Vector3 off = mouseDownHit.voxelIndex.MultiplyAllAxis(surfaceNormal.Abs());
				d.Translate(-off);
				d.Scale(Vector3Int.one + ((offsetValue + 1) * surfaceNormal));
				d.Translate(off - surfaceNormal);
			}
			else if (offsetValue < 0 && surfaceDirection.IsPositive())
			{
				Vector3 off = mouseDownHit.voxelIndex.MultiplyAllAxis(surfaceNormal.Abs());
				d.Translate(-off);
				d.Scale(Vector3Int.one + ((offsetValue - 1) * surfaceNormal));
				d.Translate(off + surfaceNormal);
			}

			Draw(d, actionColor);
		}

		protected override MapChange OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			// if (voxelEditor.Map.GetVoxel(hit.voxelIndex) == NumberSplittingUtility.emptyValue)
			//	return false;

			voxelEditor.Map.SearchPlane(_originalSide, hit.voxelIndex, hit.side, voxelEditor.SelectedAction.GetEqualityTestFunction());
			surfaceDirection = hit.side;
			surfaceNormal = hit.side.ToVectorInt();
			 
			return MapChange.None;
		}

		protected override MapChange OnVoxelCursorDrag(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			voxelEditor.RecordForUndo("Face Tool", RecordType.Map);

			offsetValue = GetOffset(voxelEditor);
			if (offsetValue == lastOffsetValue)
				return MapChange.None;


			bool isChanged = false;
			foreach (Vector3Int originalIndex in _originalSide)
			{
				Vector3Int currentEnd = originalIndex + (surfaceNormal * offsetValue);
				Vector3Int lastEnd = originalIndex + (surfaceNormal * lastOffsetValue);
				VoxelAction action = voxelEditor.SelectedAction;
				if (action == VoxelAction.Repaint)
					action = VoxelAction.Overwrite;

				if (offsetValue > 0)
				{
					if (Mathf.Abs(lastOffsetValue) > Mathf.Abs(offsetValue))// Reset original voxels
					{
						currentEnd += surfaceNormal;
						lastEnd += surfaceNormal;
						isChanged |= voxelEditor.Map.ResetRange(originalMap, currentEnd, lastEnd);
					}
					else // Set new voxels
					{
						Voxel originalValue = voxelEditor.Map.GetVoxel(originalIndex);
						isChanged |= voxelEditor.Map.SetRange(currentEnd, lastEnd, action, originalValue);
					}
				}
				else
				{
					if (Mathf.Abs(lastOffsetValue) > Mathf.Abs(offsetValue))// Reset original voxels
					{
						isChanged |= voxelEditor.Map.ResetRange(originalMap, currentEnd, lastEnd);
					}
					else // Set new voxels
					{
						currentEnd += surfaceNormal;
						Voxel originalValue = voxelEditor.Map.GetVoxel(originalIndex);
						isChanged |= voxelEditor.Map.SetRange(currentEnd, lastEnd, action, originalValue);
					}
				}
			}

			lastOffsetValue = offsetValue;
			return isChanged ? MapChange.Edit : MapChange.None;
		}

		int GetOffset(IVoxelEditor voxelEditor)
		{
			Vector3 hitPosition = mouseDownHit.hitWorldPosition;
			Vector3Int hitIndex = mouseDownHit.voxelIndex;
			Ray localRay = globalRay.Transform(voxelEditor.Transform.worldToLocalMatrix);
			Vector3 paneRight = Vector3.Cross(localRay.direction, surfaceNormal);
			Vector3 paneNormal = Vector3.Cross(paneRight, surfaceNormal);
			Plain plane = new(hitPosition, paneNormal);
			Vector3 intersectPoint = plane.Intersect(localRay);

			Line line = new(hitPosition, hitPosition + surfaceNormal);
			Vector3 cursorPoint = line.ClosestPointOnLineToPoint(intersectPoint);

			Vector3Int offsetVector = (cursorPoint - hitPosition).RoundToInt();
			offsetVector.Clamp(-hitIndex - Vector3Int.one, voxelEditor.Map.FullSize - hitIndex - Vector3Int.one);

			int absoluteOffset = offsetVector.x + offsetVector.y + offsetVector.z;
			int offsetValue = absoluteOffset * (surfaceNormal.x + surfaceNormal.y + surfaceNormal.z);
			if (voxelEditor.SelectedAction == VoxelAction.Attach)
				offsetValue = Mathf.Max(offsetValue, 0);
			if (voxelEditor.SelectedAction == VoxelAction.Erase)
				offsetValue = Mathf.Min(offsetValue, 0);
			return offsetValue;
		}

		protected override MapChange OnVoxelCursorUp(IVoxelEditor voxelEditor, VoxelHit hit)
		{
			offsetValue = 0;
			lastOffsetValue = 0;
			return MapChange.Final;
		}
	}
}
