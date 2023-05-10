using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{

	public abstract class VoxelToolHandler_SizeTools : VoxelToolHandler
	{
		int _lastStepsClamped = 0;
		public override VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => GetTransformActions(voxelEditor);

		public static int GetOriginalSize(IVoxelEditor voxelEditor, GeneralDirection3D side)
		{
			Vector3Int sizeVector = voxelEditor.HasSelection()
				? (_currentEventType == MouseEventType.None ? voxelEditor.Selection.size : _originalSelection.size)
				: (_currentEventType == MouseEventType.None ? voxelEditor.Map.FullSize : _originalMapSize);
			return sizeVector.GetAxis(side.GetAxis());
		}

		public static int GetResizeSteps(IVoxelEditor voxelEditor, int originalSize, GeneralDirection3D side)
		{
			int steps = Mathf.Max(_handleSteps, -originalSize + 1);
			if (voxelEditor.HasSelection())
			{
				Axis3D axis3D = side.GetAxis();
				int selectionSize = _originalSelection.size.GetAxis(axis3D);
				int selectionMin = _originalSelection.position.GetAxis(axis3D);
				int mapSize = voxelEditor.Map.FullSize.GetAxis(axis3D);

				if (side.IsPositive())
					steps = Mathf.Min(steps, mapSize - selectionMin - selectionSize);
				else
					steps = Mathf.Min(steps, selectionMin);
			}
			return steps;
		}

		protected static string GetStepText(int original, int steps)
		{
			string text = original.ToString();
			if (steps < 0)
				text += " - " + Mathf.Abs(steps).ToString();
			else if (steps > 0)
				text += " + " + steps.ToString();
			return text;
		}

		protected override IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{
			bool isSelectionOperation = voxelEditor.HasSelection();

			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				Vector3 position = GetMapSidePosition(voxelEditor, side);

				int originalSize = GetOriginalSize(voxelEditor, side);
				string text;

				if (_currentEventType == MouseEventType.Drag && _handleDragDirection == side)
				{
					int steps = GetResizeSteps(voxelEditor, originalSize, side);
					text = GetStepText(originalSize, steps);
				}
				else
					text = originalSize.ToString();

				yield return new VoxelHandelInfo()
				{
					coneType = HandeleConeType.Box,
					position = position,
					direction = side,
					text = text,
				};
			}
		}

		protected override bool OnHandleDown(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo) 
		{
			_lastStepsClamped = 0;
			voxelEditor.SeparateSelection();
			return false;
		}

		protected override bool OnHandleDrag(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			GeneralDirection3D direction = handleInfo.direction;

			Axis3D axis = handleInfo.direction.GetAxis();
			int oSize = _originalMapSize.GetAxis(axis);

			bool isUpEvent = _currentEventType == MouseEventType.Up;
			if (steps <= -oSize)
			{
				if (isUpEvent)
					steps = -oSize + 1;
				else
					return false;
			}

			VoxelMap map = voxelEditor.Map;
			bool isSelectionOperation = voxelEditor.HasSelection();

			int stepsClamped = steps;
			if (isSelectionOperation)
			{
				BoundsInt selection = _originalSelection;
				 
				int mapSize = _originalMapSize.GetAxis(axis);
				if (direction.IsPositive())
				{
					stepsClamped = Mathf.Clamp(steps,
						1 - selection.size.GetAxis(axis),
						mapSize - selection.max.GetAxis(axis));
				}
				else
				{
					stepsClamped = Mathf.Clamp(steps,
						1 - selection.size.GetAxis(axis),
						 selection.position.GetAxis(axis));

				}

				if (stepsClamped == _lastStepsClamped && !isUpEvent)
					return false;
				_lastStepsClamped = stepsClamped; 
			}


			Reset(voxelEditor);

			if (isUpEvent)
			{
				if (isSelectionOperation)
					voxelEditor.RecordForUndo("Selection Resized", RecordType.Map | RecordType.Editor);
				else
					voxelEditor.RecordForUndo("VoxelMap Resized", RecordType.Map | RecordType.Transform);
			}

			if (isSelectionOperation)
				return DoResizeSelection(voxelEditor, direction, stepsClamped);
		

			if ( !direction.IsPositive())
				Translate(voxelEditor, direction, stepsClamped);
			DoResizeMap(map, direction, stepsClamped);
			return true;
		}

		protected override bool OnHandleUp(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps)
		{
			OnHandleDrag(voxelEditor, handleInfo, steps);
			return false;
		}

		protected abstract void DoResizeMap(VoxelMap map, GeneralDirection3D direction, int steps);
		protected abstract bool DoResizeSelection(IVoxelEditor voxelEditor, GeneralDirection3D direction, int steps);

		protected static void ResizeSelectionContent(IVoxelEditor editor, GeneralDirection3D direction, int steps, Action<VoxelMap> mapActon)
		{
			BoundsInt selection = editor.Selection;
			VoxelMap map = editor.Map;
			ArrayVoxelMap selMap = editor.SeparateSelection();
			mapActon(selMap);
			map.ClearRange(selection);
			selection = selection.ResizeWithLimits(direction, steps, Vector3Int.zero, map.FullSize, Vector3Int.one);
			map.CopyFrom(selMap, Vector3Int.zero, selection.position, selection.size, VoxelAction.Overwrite);
			editor.Selection = selection;
		}
	}


	public class VoxelToolHandler_Resize : VoxelToolHandler_SizeTools
	{
		protected override void DoResizeMap(VoxelMap map, GeneralDirection3D direction, int steps)
		{
			Debug.Log(steps);
			map.Resize(direction, steps);
		}
		protected override bool DoResizeSelection(IVoxelEditor voxelEditor, GeneralDirection3D direction, int steps)
		{
			Reset(voxelEditor);
			Action<VoxelMap> mapActon = (map) => map.Resize(direction, steps);
			ResizeSelectionContent(voxelEditor, direction, steps, mapActon);
			return true;
		}
	}

	public class VoxelToolHandler_Repeat : VoxelToolHandler_SizeTools
	{
		protected override void DoResizeMap(VoxelMap map, GeneralDirection3D direction, int steps) =>
			 map.ResizeCanvas(direction, steps, true);
		protected override bool DoResizeSelection(IVoxelEditor voxelEditor, GeneralDirection3D direction, int steps)
		{
			Reset(voxelEditor);
			Action<VoxelMap> mapActon = (map) => map.ResizeCanvas(direction, steps, true);
			ResizeSelectionContent(voxelEditor, direction, steps, mapActon);
			return true;
		}
	}
	public class VoxelToolHandler_ResizeCanvas : VoxelToolHandler_SizeTools
	{
		protected override void DoResizeMap(VoxelMap map, GeneralDirection3D direction, int steps) =>
			 map.ResizeCanvas(direction, steps, false);
		protected override bool DoResizeSelection(IVoxelEditor voxelEditor, GeneralDirection3D direction, int steps)
		{
			voxelEditor.Selection = _originalSelection.ResizeWithLimits(direction,  steps, Vector3Int.zero, _originalMapSize, Vector3Int.one);
			return false;
		}
	}
}
