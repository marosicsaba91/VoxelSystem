#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using MUtility;

namespace VoxelSystem
{
	public partial class VoxelEditorWindow
	{
		static ArrayVoxelMap _originalMap = null; // Target Voxel Map before Mouse Down

		static Vector3Int? _mouseDownCursorVoxel;
		static Vector3Int? _lastValidMouseDragCursorVoxel;

		static VoxelHit? _cursorVoxel;

		// --------------------- SELECTION ---------------------------
		static Vector3Int _selectionStart;
		static Vector3Int _selectionEnd;
		static Vector3Int _selectionMin;
		static Vector3Int _selectionMax;
		static Vector3Int _selectionSize = Vector3Int.one;

		static void ResetSelection()
		{
			_selectionMin = Vector3Int.zero;
			_selectionMax = Vector3Int.zero;
			_selectionSize = Vector3Int.one;
		}

		// --------------------- CURSOR CONTROL ---------------------------

		void OnSceneGUI(SceneView scene)
		{
			DrawHandles();

			// Check if We have a map to work On
			if (_targetVoxelObject == null || _targetGameObject == null)
			{
				Tools.hidden = false;
				return;
			}

			bool editingIsDisabled =
				_targetVoxelObject == null ||
				_targetVoxelObject.Map == null ||
				Tool == VoxelTool.Non;

			// Enable / Disable default Unity handlers;
			Tools.hidden = !editingIsDisabled;

			// Return if editing is Not Active
			if (editingIsDisabled) return;

			// Handling the right Mouse Events, and make sure to handle nothing else
			Event e = Event.current;
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			const int leftMouseButtonIndex = 0;

			if (e.button != leftMouseButtonIndex) return;
			if (e.type is not EventType.MouseDown and not EventType.MouseUp and not EventType.MouseDrag and not EventType.MouseMove) return;

			if (_cursorTools.Contains(Tool))
			{
				// Calculate Ray from Mouse Position 
				if (_originalMap == null || _originalMap.FullSize == Vector3Int.zero) 
				{
					_originalMap = new ArrayVoxelMap();
					_originalMap.SetupFrom(_targetVoxelObject.Map); 
				}

				if (_originalMap != null)
				{
					Transform transform = _targetGameObject.transform;
					bool raycastOutside = Tool == VoxelTool.Fill;
					Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					_cursorVoxel = _originalMap.Raycast(ray, out VoxelHit hit, transform, raycastOutside) ? hit : null;
				}
				else
				{
					_cursorVoxel = null;
					return;
				}

				// Check Mouse Event
				switch (e.type)
				{
					case EventType.MouseDown:
						if (_cursorVoxel.HasValue)
							// _originalMap = usedMap;
							HandleMouseDown(_cursorVoxel.Value.voxelIndex);
						break;
					case EventType.MouseDrag:
						if (_cursorVoxel.HasValue)
							HandleMouseMove(_cursorVoxel.Value.voxelIndex);
						break;
					case EventType.MouseUp:
						Vector3Int val = _cursorVoxel.HasValue? _cursorVoxel.Value.voxelIndex : Vector3Int.zero;
						HandleMouseUp(val, _cursorVoxel.HasValue);
						break;
				}
			}

			// Avoid to change GameObject
			Selection.activeGameObject = _targetGameObject;

			// Avoid to use the event by other code
			e.Use();
		}

		void HandleMouseDown(Vector3Int voxel )
		{
			if (_basicEditTools.Contains(Tool))
			{
				_mouseDownCursorVoxel = voxel;
				_lastValidMouseDragCursorVoxel = voxel;
				_originalMap.SetupFrom(_targetVoxelObject.Map);
				_targetVoxelObject.Map.SetVoxel(_mouseDownCursorVoxel.Value, ToolToAreaAction(), SelectedPaletteIndex);
				//targetVO.RegenerateMesh();
			}
			else if (Tool == VoxelTool.Select)
			{
				_selectionStart = voxel;
				_selectionEnd = voxel;
				FreshSelection();
			}
		}

		void HandleMouseMove(Vector3Int voxel)
		{ 
			if (Tool == VoxelTool.Select)
			{
				_selectionEnd = voxel;
				FreshSelection();
			}
			else if (_mouseDownCursorVoxel.HasValue && _originalMap != null)
			{
				bool changed = voxel != _lastValidMouseDragCursorVoxel;
				if (changed)
				{
					_targetVoxelObject.Map.SetupFrom(_originalMap);
					_lastValidMouseDragCursorVoxel = voxel;
					_targetVoxelObject.Map.SetRange(_mouseDownCursorVoxel.Value, voxel, ToolToAreaAction(), SelectedPaletteIndex);
				}
			}
		}

		void HandleMouseUp(Vector3Int voxel, bool validValue)
		{
			if (Tool == VoxelTool.Select)
			{
				if (validValue)
				{
					_selectionEnd = voxel;
					FreshSelection();
				}
			}
			else if (_mouseDownCursorVoxel.HasValue && _originalMap != null)
			{
				if (validValue && _targetVoxelObject.Map.IsValidCoord(voxel))
					_lastValidMouseDragCursorVoxel = voxel; 

				if (_lastValidMouseDragCursorVoxel.HasValue)
				{
					_targetVoxelObject.Map.SetupFrom(_originalMap);
					RecordVoxelObjectForUndo(_targetVoxelObject, "VoxelMapChanged");
					_targetVoxelObject.Map.SetRange(_mouseDownCursorVoxel.Value, _lastValidMouseDragCursorVoxel.Value, ToolToAreaAction(), SelectedPaletteIndex);
				}
				_lastValidMouseDragCursorVoxel = null;
				_mouseDownCursorVoxel = null;
			}
			_originalMap = null;
		}

		void FreshSelection()
		{
			Vector3Int size = _targetVoxelObject.Map.FullSize;
			_selectionMin = new(
				   Mathf.Clamp(Mathf.Min(_selectionStart.x, _selectionEnd.x), min: 0, size.x - 1),
				   Mathf.Clamp(Mathf.Min(_selectionStart.y, _selectionEnd.y), min: 0, size.y - 1),
				   Mathf.Clamp(Mathf.Min(_selectionStart.z, _selectionEnd.z), min: 0, size.z - 1));
			_selectionMax = new(
				Mathf.Clamp(Mathf.Max(_selectionStart.x, _selectionEnd.x), min: 0, size.x - 1),
				Mathf.Clamp(Mathf.Max(_selectionStart.y, _selectionEnd.y), min: 0, size.y - 1),
				Mathf.Clamp(Mathf.Max(_selectionStart.z, _selectionEnd.z), min: 0, size.z - 1));
			_selectionSize = _selectionMax - _selectionMin + Vector3Int.one;
		}

	}
}
#endif