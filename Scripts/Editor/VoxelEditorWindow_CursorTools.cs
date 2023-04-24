#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using MUtility;

namespace VoxelSystem
{
	public partial class VoxelEditorWindow
	{
		static VoxelMap _originalMap = null; // Target Voxel Map before Mouse Down

		static Vector3Int? _mouseDownCursorVoxel;
		static Vector3Int? _lastValidMouseDragCursorVoxel;

		static InVoxelPoint? _cursorVoxel;

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
				_targetVoxelObject.Map == null ||
				_targetVoxelObject == null ||
				_targetVoxelObject.Map == null ||
				Tool == VoxelTool.Non;

			// Enable / Disable default Unity handlers;
			Tools.hidden = !editingIsDisabled;

			// Return if editing is Not Active
			if (editingIsDisabled)
			{ return; }

			// Handling the right Mouse Events, and make shure to handle nothing else
			Event e = Event.current;
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			const int leftMouseButtonIndex = 0;
			if (e.button != leftMouseButtonIndex)
			{ return; }
			if (e.type != EventType.MouseDown && e.type != EventType.MouseUp && e.type != EventType.MouseDrag && e.type != EventType.MouseMove)
			{ return; }

			if (_cursorTools.Contains(Tool))
			{
				// Calculate Ray from Mouse Position

				/* OLD */
				// Vector3 screenPosition = Event.current.mousePosition;
				// screenPosition.y = 2 + Camera.current.pixelHeight - screenPosition.y;
				// Ray ray = Camera.current.ScreenPointToRay(screenPosition);

				/* NEW */
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);


				// Get The Selected Voxel Coordinate;
				VoxelMap usedMap = _originalMap == null || _originalMap.Size == Vector3Int.zero ? _targetVoxelObject.Map : _originalMap;
				if (usedMap != null && usedMap.Raycast(ray, out InVoxelPoint hit, _targetGameObject.transform, Tool == VoxelTool.Attach))
				{
					_cursorVoxel = hit;
				}
				else
				{
					_cursorVoxel = null;
				}

				// Check Mouse Event
				Vector3Int? showedCursorVoxel = _cursorVoxel.HasValue ? (Vector3Int?)_cursorVoxel.Value.voxel : null;

				if (e.type == EventType.MouseDown && _cursorVoxel != null)
				{ HandleMouseDown(showedCursorVoxel); }
				else if (e.type == EventType.MouseDrag)
				{ HandleMouseMove(showedCursorVoxel); }
				else if (e.type == EventType.MouseUp)
				{ HandleMouseUp(showedCursorVoxel); }
			}

			// Avoid to change gameobject
			Selection.activeGameObject = _targetGameObject;

			// Avoid to use the event by other code
			e.Use();
		}

		void HandleMouseDown(Vector3Int? voxel)
		{
			if (voxel == null)
			{ return; }
			if (_basicEditTools.Contains(Tool))
			{
				_mouseDownCursorVoxel = voxel;
				_lastValidMouseDragCursorVoxel = voxel;
				_originalMap = _targetVoxelObject.Map.GetCopy();
				_targetVoxelObject.Map.Set(_mouseDownCursorVoxel.Value, ToolToAreaAction(), SelectedPaletteIndex);
				//targetVO.RegenerateMesh();
			}
			else if (Tool == VoxelTool.Select)
			{
				_selectionStart = voxel.Value;
				_selectionEnd = voxel.Value;
				FreshSelection();
			}
		}

		void HandleMouseMove(Vector3Int? voxel)
		{
			if (!voxel.HasValue)
			{ return; }
			if (Tool == VoxelTool.Select)
			{
				_selectionEnd = voxel.Value;
				FreshSelection();
			}
			else if (_mouseDownCursorVoxel.HasValue && _originalMap != null)
			{
				bool changed = voxel != _lastValidMouseDragCursorVoxel;
				if (changed)
				{
					_targetVoxelObject.Map = _originalMap.GetCopy();
					_lastValidMouseDragCursorVoxel = voxel;
					_targetVoxelObject.Map.SetRange(_mouseDownCursorVoxel.Value, voxel.Value, ToolToAreaAction(), SelectedPaletteIndex);
				}
			}
		}

		void HandleMouseUp(Vector3Int? voxel)
		{
			if (Tool == VoxelTool.Select)
			{
				if (voxel.HasValue)
				{
					_selectionEnd = voxel.Value;
					FreshSelection();
				}
			}
			else if (_mouseDownCursorVoxel.HasValue && _originalMap != null)
			{
				if (voxel.HasValue && _targetVoxelObject.IsValidCoord(voxel.Value))
				{ _lastValidMouseDragCursorVoxel = voxel; }
				if (_lastValidMouseDragCursorVoxel.HasValue)
				{
					_targetVoxelObject.Map = _originalMap.GetCopy();
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
			Vector3Int size = _targetVoxelObject.Map.Size;
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