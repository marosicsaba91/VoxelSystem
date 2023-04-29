#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MUtility;
using System;

namespace VoxelSystem
{
	public partial class VoxelEditorWindow
	{
		static GeneralDirection3D? _handleDirection = null;
		static Vector3Int _handleVector = Vector3Int.zero;
		static int _handleSteps;

		void HandleArrowHandles()
		{
			if (_editorComponent == null) return;
			if (!SelectedTool.IsTransformTool()) return;

			VoxelMap map = _editorComponent.Map;
			if (map == null) return;

			Vector3Int size = map.FullSize;

			Vector3 lossyScale = _editorComponent.transform.lossyScale;
			float arrowSize =
				(new float[] { size.x, size.y, size.z }).Average() *
				MathHelper.Average(lossyScale.x, lossyScale.y, lossyScale.z)
				/ 10;
			float arrowSpacing = 2 * arrowSize;

			// GeneralDirection3D focusedDir = (GeneralDirection3D)(HandleUtility.nearestControl - 100);

			for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
			{
				GeneralDirection3D side = DirectionUtility.generalDirection3DValues[i];
				if ((SelectedTool == VoxelTool.Turn || SelectedTool == VoxelTool.Mirror) && !IsMapSideSeen(side))
				{ continue; }
				Vector3 worldPos = GetHandlePos(side, size, arrowSpacing);

				if (SelectedTool == VoxelTool.Turn)
				{
					for (int j = 0; j < DirectionUtility.generalDirection3DValues.Length; j++)
					{
						GeneralDirection3D side2 = DirectionUtility.generalDirection3DValues[j];
						if (side.GetAxis() == side2.GetAxis())
						{ continue; }

						GeneralDirection3D d = side.LeftHandedRotate(side2.GetAxis(), side2.IsPositive() ? 1 : -1);
						Axis3D a2 = side2.GetAxis();
						Vector3 pos = worldPos + (arrowSpacing * _targetGameObject.transform.TransformVector(d.ToVector()));

						HandleOneArrowHandle(arrowSize, a2, d, pos, side, SelectedTool);
					}
				}
				else
				{ HandleOneArrowHandle(arrowSize, side.GetAxis(), side, worldPos, side, SelectedTool); }
			}
		}

		void HandleOneArrowHandle(float arrowSize, Axis3D axis, GeneralDirection3D dir, Vector3 worldPos, GeneralDirection3D side, VoxelTool tool)
		{
			Quaternion worldRotation = GetHandleDir(dir);

			Color color = axis.GetAxisColor();
			Color focused = (color + Color.white) / 2f;
			Handles.color = color;

			Vector3 handlePos = worldPos;
			if (_handleDirection.HasValue && _handleDirection.Value == dir)
			{
				GUIStyle style = new();
				style.normal.textColor = (color + Color.black) / 2;
				style.fontSize = 15;

				string label = (_handleSteps >= 0 ? "+" : "") + _handleSteps.ToString();

				if (SelectedTool.IsSizeTool())
				{
					label += " : " + _editorComponent.Map.GetSize(axis);
				}

				Handles.Label(handlePos + new Vector3(x: 0, y: 5, z: 0), label, style);
			}
			AdvancedHandles.HandleResult handleResult = AdvancedHandles.Handle(handlePos, worldRotation, arrowSize, Handles.ConeHandleCap, color, focused, color);


			switch (handleResult.handleEvent)
			{
				case HandleEvent.LmbPress:
					if (tool == VoxelTool.Turn)
					{
						Turn(axis, dir, side);
					}
					else
					if (tool == VoxelTool.Mirror)
					{
						Mirror(axis);
					}
					else
					{
						_handleDirection = dir;
						_handleVector = Vector3Int.zero;
						StartArrowHandleAction(dir);
					}
					break;
				case HandleEvent.LmbDrag:
					if (tool == VoxelTool.Turn || SelectedTool == VoxelTool.Mirror)
					{ break; }

					_handleSteps = (int)Vector3.Dot(_targetGameObject.transform.InverseTransformVector(handleResult.IsDragged), dir.ToVector());

					Vector3Int vec = dir.ToVectorInt() * _handleSteps;
					bool changed = _handleVector != vec;
					_handleVector = vec;
					if (changed)
						DragArrowHandleAction(_handleDirection.Value, _handleSteps);

					break;
				case HandleEvent.LmbRelease:
				case HandleEvent.LmbClick:
				case HandleEvent.LmbDoubleClick:
					if (tool == VoxelTool.Turn || SelectedTool == VoxelTool.Mirror)
					{ break; }

					_handleSteps = (int)Vector3.Dot(_targetGameObject.transform.InverseTransformVector(handleResult.IsDragged), dir.ToVector());
					_handleSteps = Mathf.Max(-_originalMap.GetSize(axis) + 1, _handleSteps);
					_handleVector = dir.ToVectorInt() * _handleSteps;
					ReleaseArrowHandleAction(_handleDirection.Value, _handleSteps);
					_handleDirection = null;
					_handleSteps = 0;
					_handleVector = Vector3Int.zero;

					break;
					//default:					
					//		throw new ArgumentException("Invalid handle event: " + handleResult.handleEvent);
			}
		}

		void Mirror(Axis3D axis)
		{
			RecordVoxelObjectForUndo(_editorComponent, "VoxelMapMirrored");
			_editorComponent.Map.Mirror(axis);
		}

		void Turn(Axis3D axis, GeneralDirection3D dir, GeneralDirection3D side)
		{
			GeneralDirection3D d = side.IsPositive() ? dir : dir.Opposite();
			bool leftHandPositive = d.IsPositive();

			if ((axis == Axis3D.Z && d.GetAxis() == Axis3D.Y) ||
				(axis == Axis3D.X && d.GetAxis() == Axis3D.Z) ||
				(axis == Axis3D.Y && d.GetAxis() == Axis3D.X))
				leftHandPositive = !leftHandPositive;

			RecordVoxelObjectForUndo(_editorComponent, "VoxelMapTurned");
			_editorComponent.Map.Turn(axis, leftHandPositive);
		}

		Vector3 _originalPos;
		void StartArrowHandleAction(GeneralDirection3D direction)
		{
			_originalPos = _targetGameObject.transform.localPosition;
			_originalMap ??= new ArrayVoxelMap();
			_originalMap.SetupFrom(_editorComponent.Map);
		}

		void ReleaseArrowHandleAction(GeneralDirection3D direction, int steps)
		{
			if (SelectedTool == VoxelTool.Move)
			{
				Undo.RecordObject(_targetGameObject.transform, "VoxelMapMoved");
				Translate(direction, steps);
			}
			else if (SelectedTool.IsSizeTool())
			{
				ResizeMapRelease(direction, steps);
			}
			_originalMap = null;
		}

		void ResizeMapRelease(GeneralDirection3D direction, int steps)
		{
			if (_originalMap != null)
			{
				steps = Mathf.Max(-_originalMap.GetSize(direction.GetAxis()) + 1, steps);

				_editorComponent?.Map?.SetupFrom(_originalMap);

				RecordVoxelObjectForUndo(_editorComponent, "VoxelMapResized");

				_editorComponent.Map.Resize(direction, steps, ToolToResizeType(SelectedTool));

				if (!direction.IsPositive())
				{
					_targetGameObject.transform.localPosition = _originalPos;
					Undo.RecordObject(_targetGameObject.transform, "VoxelMapMoved");
					Translate(direction, steps);

				}
			}
		}

		void DragArrowHandleAction(GeneralDirection3D direction, int steps)
		{
			if (SelectedTool == VoxelTool.Move)
			{
				Undo.RecordObject(_targetGameObject.transform, "VoxelMapMoved");
				Translate(direction, steps);
			}
			else if (SelectedTool.IsSizeTool())
			{
				ResizeMapDrag(direction, steps);
			}
		}

		void Translate(GeneralDirection3D direction, int steps)
		{
			if (_targetGameObject.transform.parent != null)
			{
				_targetGameObject.transform.localPosition = _originalPos +
					_targetGameObject.transform.parent.InverseTransformVector(_targetGameObject.transform.TransformVector((direction.ToVector() * steps)));
			}
			else
			{
				_targetGameObject.transform.localPosition = _originalPos + _targetGameObject.transform.TransformVector((direction.ToVector() * steps));
			}
		}

		void ResizeMapDrag(GeneralDirection3D direction, int steps)
		{
			steps = Mathf.Max(-_originalMap.GetSize(direction.GetAxis()) + 1, steps);
			if (!direction.IsPositive())
			{
				Translate(direction, steps);
			}
			_editorComponent.Map.SetupFrom(_originalMap);
			_editorComponent.Map.Resize(direction, steps, ToolToResizeType(SelectedTool));
		}

		static VoxelMap.ResizeType ToolToResizeType(VoxelTool tool) => tool == VoxelTool.Rescale ? VoxelMap.ResizeType.Rescale :
				tool == VoxelTool.Repeat ? VoxelMap.ResizeType.Repeat :
				VoxelMap.ResizeType.Resize;

		static Vector3 GetHandlePos(GeneralDirection3D side, Vector3 size, float arrowSpacing)
		{
			Vector3 dir = side.ToVector();
			Vector3 position =
				side == GeneralDirection3D.Right ? new(size.x, size.y / 2f, size.z / 2f) :
				side == GeneralDirection3D.Up ? new(size.x / 2f, size.y, size.z / 2f) :
				side == GeneralDirection3D.Forward ? new(size.x / 2f, size.y / 2f, size.z) :
				side == GeneralDirection3D.Left ? new(x: 0, size.y / 2f, size.z / 2f) :
				side == GeneralDirection3D.Down ? new(size.x / 2f, y: 0, size.z / 2f) :
				side == GeneralDirection3D.Back ? new(size.x / 2f, size.y / 2f, z: 0) :
				Vector3.zero;

			return _targetGameObject.transform.TransformPoint(position) + (_targetGameObject.transform.TransformDirection(dir) * arrowSpacing);
		}

		static Quaternion GetHandleDir(GeneralDirection3D side)
		{
			Vector3 dir = side.ToVector();
			Vector3 worldDir = _targetGameObject.transform.TransformVector(dir);
			return Quaternion.LookRotation(worldDir, worldDir);
		}
	}
}
#endif