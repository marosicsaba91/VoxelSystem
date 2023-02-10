#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using MUtility;

namespace VoxelSystem
{
    public partial class VoxelEditorWindow 
    {
        static readonly Color BorderColor = Color.white;
        static readonly Color InvalidMaterialColor = Color.gray;

        void DrawHandles()
        {
            if (_targetVoxelObject == null || _targetGameObject == null) { return; }

            VoxelMap map = _targetVoxelObject.Map;
            if (map == null) { return; }
            Vector3 size = map.Size;

            
            DrawInSceneWindowGUI();
            DrawBounds(size);
            DrawCursor(_cursorVoxel);
            HandleArrowHandles();
        }

        static void DrawBounds(Vector3 size)
        {
            if (Tool == VoxelTool.Select)
            {
                Handles.color = BorderColor; 
                DrawWireCube_InWorld(_targetGameObject.transform, _selectionMin, _selectionSize);

                Handles.color = new(BorderColor.r, BorderColor.g, BorderColor.b, BorderColor.a / 4f);
                DrawWireCube_InWorld(_targetGameObject.transform, Vector3.zero, size);
            }
            else
            {
                Handles.color = BorderColor;
                DrawWireCube_InWorld(_targetGameObject.transform, Vector3.zero, size);
            }
        }
        
        static readonly Color C = new(r: 1,g: 0,b: 0);
        static void DrawCursor(VoxelRayCollidingInfo? cursorVoxel)
        {
            if (cursorVoxel == null) { return; }
            if (!_cursorTools.Contains(Tool)) { return; }

            Transform transform = _targetGameObject.transform;

            Color cursorColor = C;
            Color softCursorColor = new(cursorColor.r, cursorColor.g, cursorColor.b, cursorColor.a / 4f);

            Handles.color = cursorColor;
            DrawVoxelSide_InWorld(transform, cursorVoxel.Value.voxel, cursorVoxel.Value.side, size: 1f);
            DrawVoxelSide_InWorld(transform, cursorVoxel.Value.voxel, cursorVoxel.Value.side, size: 0.75f);
            DrawVoxelSide_InWorld(transform, cursorVoxel.Value.voxel, cursorVoxel.Value.side, size: 0.5f);
            DrawVoxelSide_InWorld(transform, cursorVoxel.Value.voxel, cursorVoxel.Value.side, size: 0.25f);


            Handles.color = softCursorColor;
            Vector3Int voxel = cursorVoxel.Value.voxel;
            if (_targetVoxelObject.IsValidCoord(voxel))
            {
                DrawVoxel_InWorld(transform, voxel);
            }
        }

       
        static void DrawInSceneWindowGUI()
        {
            /*
            Handles.BeginGUI();

            if (GUILayout.Button("???", GUILayout.Width(100)))
            {
                Debug.Log("YIP");
            }
            Handles.EndGUI();
            */
        }
        

        static void DrawVoxel_InWorld(Transform transform, Vector3Int localCoordinate)
        {
            DrawWireCube_InWorld(transform, localCoordinate, Vector3.one);
        }

        static void DrawWireCube_InWorld(Transform transform, Vector3 origin, Vector3 size)
        {
            float x0 = origin.x;
            float x1 = origin.x + size.x;
            float y0 = origin.y;
            float y1 = origin.y + size.y;
            float z0 = origin.z;
            float z1 = origin.z + size.z;
            Vector3 p000 = transform.TransformPoint(new(x0, y0, z0));
            Vector3 p001 = transform.TransformPoint(new(x0, y0, z1));
            Vector3 p010 = transform.TransformPoint(new(x0, y1, z0));
            Vector3 p011 = transform.TransformPoint(new(x0, y1, z1));
            Vector3 p100 = transform.TransformPoint(new(x1, y0, z0));
            Vector3 p101 = transform.TransformPoint(new(x1, y0, z1));
            Vector3 p110 = transform.TransformPoint(new(x1, y1, z0));
            Vector3 p111 = transform.TransformPoint(new(x1, y1, z1));

            Handles.DrawLine(p000, p010); Handles.DrawLine(p010, p110); Handles.DrawLine(p110, p100); Handles.DrawLine(p100, p000);
            Handles.DrawLine(p000, p001); Handles.DrawLine(p010, p011); Handles.DrawLine(p100, p101); Handles.DrawLine(p110, p111);
            Handles.DrawLine(p001, p011); Handles.DrawLine(p011, p111); Handles.DrawLine(p111, p101); Handles.DrawLine(p101, p001);
        }

        static void DrawVoxelSide_InWorld(Transform transform, Vector3Int localCoordinate, GeneralDirection3D side, float size)
        {
            Vector3 x =
                side == GeneralDirection3D.Up || side == GeneralDirection3D.Down ? new(size * 0.5f, y: 0, z: 0) :
                side == GeneralDirection3D.Left || side == GeneralDirection3D.Right ? new(x: 0, y: 0, size * 0.5f) :
                side == GeneralDirection3D.Forward || side == GeneralDirection3D.Back ? new(x: 0, size * 0.5f, z: 0) : Vector3.zero;
            Vector3 y =
                side == GeneralDirection3D.Up || side == GeneralDirection3D.Down ? new(x: 0, y: 0, size * 0.5f) :
                side == GeneralDirection3D.Left || side == GeneralDirection3D.Right ? new(x: 0, size * 0.5f, z: 0) :
                side == GeneralDirection3D.Forward || side == GeneralDirection3D.Back ? new(size * 0.5f, y: 0, z: 0) : Vector3.zero;
            Vector3 offset =
                side == GeneralDirection3D.Up ? new(x: 0, y: 0.5f, z: 0) :
                side == GeneralDirection3D.Down ? new(x: 0, y: -0.5f, z: 0) :
                side == GeneralDirection3D.Left ? new(x: -0.5f, y: 0, z: 0) :
                side == GeneralDirection3D.Right ? new(x: 0.5f, y: 0, z: 0) :
                side == GeneralDirection3D.Forward ? new(x: 0, y: 0, z: 0.5f) :
                side == GeneralDirection3D.Back ? new(x: 0, y: 0, z: -0.5f) : Vector3.zero;

            Vector3 halfSize = new(x: 0.5f, y: 0.5f, z: 0.5f);
            Vector3[] points = new[]{
                transform.TransformPoint( localCoordinate + offset + x + y + halfSize),
                transform.TransformPoint( localCoordinate + offset + x + -y + halfSize),
                transform.TransformPoint( localCoordinate + offset + -x + -y + halfSize),
                transform.TransformPoint( localCoordinate + offset + -x + y + halfSize)
            };

            DrawPolygon(points);
        }

        static void DrawPolygon(Vector3[] points, int drawableleFragments = 1, bool closeLine = true)
        {
            if (points.Length <= 1) { return; }
            if (drawableleFragments < 1) { drawableleFragments = 1; }

            for (int i = 0; i < points.Length - 1; i += drawableleFragments)
            {
                Handles.DrawLine(points[i], points[i + 1]);
            }

            if (closeLine && (drawableleFragments == 1 || points.Length % drawableleFragments + 1 == 0))
            {
                Handles.DrawLine(points[0], points.Last());
            }
        }
    }
}
#endif