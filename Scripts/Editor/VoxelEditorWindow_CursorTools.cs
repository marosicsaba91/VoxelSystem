#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MUtility;

namespace VoxelSystem
{
    public partial class VoxelEditorWindow
    {
        struct VoxelRayCollidingInfo
        {
            public Vector3Int voxel;
            public GeneralDirection3D side;
            public Vector3 point;
        }
        
        static VoxelMap _originalMap = null; // Target Voxel Map before Mouse Down

        static Vector3Int? _mouseDownCursorVoxel;
        static Vector3Int? _lastValidMouseDragCursorVoxel;

        static VoxelRayCollidingInfo? _cursorVoxel;

        // --------------------- SELECTION ---------------------------
        static Vector3Int _selectionStart;
        static Vector3Int _selectionEnd;
        static Vector3Int _selectionMin;
        static Vector3Int _selectionMax;
        static Vector3Int _selectionSize = Vector3Int.one;

        static void ResetSelection() {
            _selectionMin = Vector3Int.zero;
            _selectionMax = Vector3Int.zero;
            _selectionSize = Vector3Int.one;
        }

        // --------------------- CURSOR CONTROL ---------------------------

        void OnSceneGUI(SceneView scene)
        {
            DrawHandles();

            // Check if We have a map to work On
            if (_targetVoxelObject == null || _targetGameObject==null) {
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
            if (editingIsDisabled) { return; }
            
            // Handling the right Mouse Events, and make shure to handle nothing else
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            const int leftMouseButtonIndex = 0;
            if (e.button != leftMouseButtonIndex) { return; }
            if (e.type != EventType.MouseDown && e.type != EventType.MouseUp && e.type != EventType.MouseDrag && e.type != EventType.MouseMove) { return; }

            if (_cursorTools.Contains(Tool))
            {
                // Calculate Ray from Mouse Position
                
                /* OLD */
                // Vector3 screenPosition = Event.current.mousePosition;
                // screenPosition.y = 2 + Camera.current.pixelHeight - screenPosition.y;
                // Ray ray = Camera.current.ScreenPointToRay(screenPosition);

                /* NEW */
                Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);

                
                // Get The Selected Voxel Coordinate;
                VoxelMap usedMap = _originalMap == null || _originalMap.Size == Vector3Int.zero ? _targetVoxelObject.Map : _originalMap;
                _cursorVoxel = CalculateCursorCoordinateOfMap(ray, _targetGameObject.transform, usedMap, Tool == VoxelTool.Attach);

                // Check Mouse Event
                Vector3Int? showedCursorVoxel = _cursorVoxel.HasValue ? (Vector3Int?)_cursorVoxel.Value.voxel : null;

                if (e.type == EventType.MouseDown && _cursorVoxel != null) { HandleMouseDown(showedCursorVoxel); }
                else if (e.type == EventType.MouseDrag) { HandleMouseMove(showedCursorVoxel); }
                else if (e.type == EventType.MouseUp) { HandleMouseUp(showedCursorVoxel); }
            }

            // Avoid to change gameobject
            Selection.activeGameObject = _targetGameObject;

            // Avoid to use the event by other code
            e.Use();
        }

        void HandleMouseDown(Vector3Int? voxel)
        {
            if (voxel == null) { return; }
            if (_basicEditTools.Contains(Tool))
            {
                _mouseDownCursorVoxel = voxel;
                _lastValidMouseDragCursorVoxel = voxel;
                _originalMap = _targetVoxelObject.Map.GetCopy();
                _targetVoxelObject.Map.Set(_mouseDownCursorVoxel.Value, ToolToAreaAction(), SelectedPaletteIndex);
                //targetVO.RegenerateMesh();
            }
            else if (Tool == VoxelTool.Select) {
                _selectionStart = voxel.Value;
                _selectionEnd = voxel.Value;
                FreshSelection();
            }
        }

        void HandleMouseMove(Vector3Int? voxel)
        {
            if (!voxel.HasValue) { return; }
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
                    _selectionEnd = voxel.Value ;
                    FreshSelection();
                }
            }
            else if (_mouseDownCursorVoxel.HasValue && _originalMap != null)
            {
                if (voxel.HasValue && _targetVoxelObject.IsValidCoord(voxel.Value)) { _lastValidMouseDragCursorVoxel = voxel; }
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

        void FreshSelection() {
            Vector3Int size = _targetVoxelObject.Map.Size;
            _selectionMin = new(
                   Mathf.Clamp(Mathf.Min(_selectionStart.x, _selectionEnd.x), min: 0,  size.x - 1),
                   Mathf.Clamp(Mathf.Min(_selectionStart.y, _selectionEnd.y), min: 0,  size.y - 1),
                   Mathf.Clamp(Mathf.Min(_selectionStart.z, _selectionEnd.z), min: 0,  size.z - 1));
            _selectionMax = new(
                Mathf.Clamp(Mathf.Max(_selectionStart.x, _selectionEnd.x), min: 0, size.x - 1),
                Mathf.Clamp(Mathf.Max(_selectionStart.y, _selectionEnd.y), min: 0, size.y - 1),
                Mathf.Clamp(Mathf.Max(_selectionStart.z, _selectionEnd.z), min: 0, size.z - 1));
            _selectionSize = _selectionMax - _selectionMin + Vector3Int.one;
        }

        static VoxelRayCollidingInfo? CalculateCursorCoordinateOfMap(Ray globalRay, Transform voxelObjectTransform, VoxelMap map, bool attach)
        {
            if (voxelObjectTransform == null || map == null) { return null; }

            // Transform the Vector by the VoxelObject Transform
            Ray transformedRay = new(voxelObjectTransform.InverseTransformPoint(globalRay.origin), voxelObjectTransform.InverseTransformVector(globalRay.direction));

            // Try Find the entry point
            VoxelRayCollidingInfo? voxelMapEntry = FindEntryPointToVoxelMap(transformedRay, map);

            // We searche for teh voxel on the line until We find a filled voxel
            return CalculateFirstEmptyVoxelInTheRayPath(voxelMapEntry, transformedRay.direction, map, attach);
        }

        static VoxelRayCollidingInfo? FindEntryPointToVoxelMap(Ray ray, VoxelMap map)
        {

            GeneralDirection3D[] sides = DirectionUtility.generalDirection3DValues;

            Vector3? entryPoint = null;
            GeneralDirection3D entrySide = GeneralDirection3D.Up;
            Vector3Int firstFoundVoxel = Vector3Int.zero;
            Vector3Int mapSize = map.Size;
            Vector3Int sideNormal;

            for (int i = 0; i < sides.Length; i++)
            {
                entrySide = sides[i];
                sideNormal = entrySide.ToVectorInt();
                bool positive = sideNormal.x > 0 || sideNormal.y > 0 || sideNormal.z > 0;
                Vector3 planeOrigin = positive ? Vector3.zero : mapSize;
                entryPoint = RayIntersectPlane(sideNormal, planeOrigin, ray);

                if (entryPoint != null)
                {
                    if (!positive)
                    {
                        entryPoint = (mapSize + entryPoint);
                        firstFoundVoxel = new((int)entryPoint.Value.x, (int)entryPoint.Value.y,
                            (int)entryPoint.Value.z);
                        firstFoundVoxel += sideNormal;
                    }
                    else
                    {
                        firstFoundVoxel = new((int)entryPoint.Value.x, (int)entryPoint.Value.y,
                            (int)entryPoint.Value.z);
                    }

                    if (_targetVoxelObject.Map.IsValidCoord(firstFoundVoxel))
                    {
                        const float epsylon = 0.001f;
                        if (entryPoint.Value.x > -epsylon && entryPoint.Value.x < mapSize.x + epsylon &&
                            entryPoint.Value.y > -epsylon && entryPoint.Value.y < mapSize.y + epsylon &&
                            entryPoint.Value.z > -epsylon && entryPoint.Value.z < mapSize.z + epsylon)
                        {

                            return new VoxelRayCollidingInfo()
                            {
                                voxel = firstFoundVoxel,
                                point = entryPoint.Value,
                                side = entrySide.Opposite()
                            };
                        }
                    }
                }
            }

            return null;
        }

        static Vector3? RayIntersectPlane(Vector3 planeNormal, Vector3 planeOrigin, Ray ray)
        {
            // assuming vectors are all normalized
            float denom = Vector3.Dot(planeNormal, ray.direction);
            const float epsylon = 0.0001f;
            if (denom < epsylon) { return null; }

            Vector3 p0L0 = planeOrigin - ray.origin;
            float t = Vector3.Dot(p0L0, planeNormal) / denom;
            return (ray.origin + (ray.direction * t)) - planeOrigin;
        }

        static VoxelRayCollidingInfo? CalculateFirstEmptyVoxelInTheRayPath(VoxelRayCollidingInfo? entry, Vector3 rayDirection, VoxelMap map, bool attach)
        {
            if (entry == null || map == null) { return null; }
            Vector3Int entryVoxel = entry.Value.voxel;
            Vector3 entryPoint = entry.Value.point;
            GeneralDirection3D entrySide = entry.Value.side;
            if (!map.IsValidCoord(entryVoxel)) { return null; }


            var cursorPathVoxels = new List<Vector3Int>(); 
            Vector3Int lastFoundVoxel = entryVoxel;

            VoxelRayCollidingInfo cursor = new();
            // Side found outside the box
            if (map.Get(entryVoxel).IsFilled)
            {
                cursor.side = entrySide;
                cursor.voxel = lastFoundVoxel;
                cursor.point = entryPoint;
                return cursor;
            }

            // In the Cube
            bool xIsPositive = rayDirection.x > 0;
            bool yIsPositive = rayDirection.y > 0;
            bool zIsPositive = rayDirection.z > 0;
            Vector3 lastIntersect = entryPoint;

            do
            {
                cursor.point = lastIntersect;
                cursor.voxel = lastFoundVoxel;
                cursorPathVoxels.Add(lastFoundVoxel);

                var distanceToDo = new Vector3(
                    xIsPositive ? Ceil(lastIntersect.x) - lastIntersect.x : Floor(lastIntersect.x) - lastIntersect.x,
                    yIsPositive ? Ceil(lastIntersect.y) - lastIntersect.y : Floor(lastIntersect.y) - lastIntersect.y,
                    zIsPositive ? Ceil(lastIntersect.z) - lastIntersect.z : Floor(lastIntersect.z) - lastIntersect.z
                );
                var timeToIntersect = new Vector3(
                    distanceToDo.x / rayDirection.x,
                    distanceToDo.y / rayDirection.y,
                    distanceToDo.z / rayDirection.z);

                float minTime = Mathf.Min(timeToIntersect.x, timeToIntersect.y, timeToIntersect.z);
                if (Math.Abs(minTime - timeToIntersect.x) < epsilon)
                {
                    lastFoundVoxel.x += xIsPositive ? 1 : -1;
                    cursor.side = xIsPositive ? GeneralDirection3D.Right : GeneralDirection3D.Left;
                }
                else if (Math.Abs(minTime - timeToIntersect.y) < epsilon)
                {
                    lastFoundVoxel.y += yIsPositive ? 1 : -1;
                    cursor.side = yIsPositive ? GeneralDirection3D.Up : GeneralDirection3D.Down;
                }
                else if (Math.Abs(minTime - timeToIntersect.z) < epsilon)
                {
                    lastFoundVoxel.z += zIsPositive ? 1 : -1;
                    cursor.side = zIsPositive ? GeneralDirection3D.Forward : GeneralDirection3D.Back;
                }

                lastIntersect += minTime * rayDirection;

            }
            while (
                _targetVoxelObject.IsValidCoord(lastFoundVoxel) && 
                map.Get(lastFoundVoxel.x, lastFoundVoxel.y, lastFoundVoxel.z).IsEmpty);

            cursor.voxel = attach ? cursor.voxel : cursor.voxel + cursor.side.ToVectorInt();
            cursor.side = attach ? cursor.side : cursor.side.Opposite();
            return cursor;
        }

        static int Ceil(float f)
        {
            if (f % 1f > 1f - epsilon) { return ((int)f) + 2; }
            return ((int)f) + 1;
        }

        static int Floor(float f)
        { 
            if (f % 1f < epsilon) { return ((int)f) - 1; }
            return (int)f;
        }
            const float epsilon = 0.0001f;
    }
}
#endif