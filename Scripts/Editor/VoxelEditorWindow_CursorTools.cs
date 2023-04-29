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
			if (_editorComponent == null || _targetGameObject == null)
			{
				Tools.hidden = false;
				return;
			}

			bool editingIsDisabled =
				_editorComponent == null ||
				_editorComponent.Map == null ||
				SelectedTool == VoxelTool.None;

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

			if (SelectedTool.IsCursorTool())
			{
				if (_originalMap == null || _originalMap.FullSize == Vector3Int.zero) 
				{
					_originalMap = new ArrayVoxelMap();
					_originalMap.SetupFrom(_editorComponent.Map); 
				}

				// Calculate Ray from Mouse Position 
				if (_originalMap != null)
				{
					Transform transform = _targetGameObject.transform;
					bool raycastOutside = SelectedVoxelAction.IsAdditive();
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
			if (SelectedTool != VoxelTool.Box) return;

			if (SelectedVoxelAction == VoxelAction.Select)
			{
				_selectionStart = voxel;
				_selectionEnd = voxel;
				FreshSelection();
			}
			else
			{
				VoxelMap map = _editorComponent.Map;
				_mouseDownCursorVoxel = voxel;
				_lastValidMouseDragCursorVoxel = voxel;
				_originalMap.SetupFrom(map);
				bool changed = map.SetVoxel(_mouseDownCursorVoxel.Value, SelectedVoxelAction, SelectedPaletteIndex);
				if(changed)
					map.MapChanged();
			} 
		}

		void HandleMouseMove(Vector3Int voxel)
		{
			if (SelectedVoxelAction == VoxelAction.Select)
			{
				_selectionEnd = voxel;
				FreshSelection();
			}
			else if (_mouseDownCursorVoxel.HasValue && _originalMap != null)
			{
				bool changed = voxel != _lastValidMouseDragCursorVoxel;
				if (changed)
				{
					VoxelMap map = _editorComponent.Map;
					map.SetupFrom(_originalMap);
					_lastValidMouseDragCursorVoxel = voxel;
					bool change = map.SetRange(_mouseDownCursorVoxel.Value, voxel, SelectedVoxelAction, SelectedPaletteIndex);
					if(change)
						map.MapChanged();
				}
			}
		}

		void HandleMouseUp(Vector3Int voxel, bool validValue)
		{
			if (SelectedVoxelAction == VoxelAction.Select)
			{
				if (validValue)
				{
					_selectionEnd = voxel;
					FreshSelection();
				}
			}
			else if (_mouseDownCursorVoxel.HasValue && _originalMap != null)
			{
				if (validValue && _editorComponent.Map.IsValidCoord(voxel))
					_lastValidMouseDragCursorVoxel = voxel; 

				if (_lastValidMouseDragCursorVoxel.HasValue)
				{
					VoxelMap map = _editorComponent.Map;
					map.SetupFrom(_originalMap);
					RecordVoxelObjectForUndo(_editorComponent, "VoxelMapChanged");
					bool change = map.SetRange(_mouseDownCursorVoxel.Value, _lastValidMouseDragCursorVoxel.Value, SelectedVoxelAction, SelectedPaletteIndex);
					if (change)
						map.MapChanged();
				}
				_lastValidMouseDragCursorVoxel = null;
				_mouseDownCursorVoxel = null;
			}
			_originalMap = null;
		}

		void FreshSelection()
		{
			Vector3Int size = _editorComponent.Map.FullSize;
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