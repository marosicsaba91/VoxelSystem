#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using UnityEditor;
using static VoxelSystem.VoxelPalette;

namespace VoxelSystem
{
    public partial class VoxelEditorWindow 
    {
        // Add menu named "Voxel Editor" to the Tools menu
        [MenuItem("Tools/Voxel Editor")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            VoxelEditorWindow window = (VoxelEditorWindow) GetWindow(typeof(VoxelEditorWindow));
            window.Show();
        }

        const float fullW = 185;
        const float separatorW = 3f;
        const float smallButtonW = 25f;
        static readonly GUILayoutOption _columnWidth = GUILayout.Width(fullW / 1 - separatorW);
        static readonly GUILayoutOption _smallButtonWidth = GUILayout.Width(smallButtonW);

        void OnGUI()
        {
            GUIStyle boldMiddle = new GUIStyle(EditorStyles.boldLabel);
            boldMiddle.alignment = TextAnchor.MiddleCenter;
            if (_targetVoxelObject == null)
            {
                EditorGUILayout.Space();
                GUILayout.Label("No VoxleObject Selected!\nSelect a VoxleObject to edit!", boldMiddle);

                return;
            }

            if (position.height < position.width) EditorGUILayout.BeginHorizontal();

            // Map            
            EditorGUILayout.BeginVertical(_columnWidth);
            string label = "Map";
            if (_targetVoxelObject.HasConnectedMap())
            {
                label += ": " + _targetVoxelObject.ConnectedMap.name;
            }

            GUILayout.Label(label, boldMiddle);
            Vector3Int s = _targetVoxelObject.Map.Size;
            GUIStyle small = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label(" (" + s.x + "x" + s.y + "x" + s.z + ")", small);

            if (_targetVoxelObject.Builder == null)
            {

                EditorGUILayout.Space();
                GUILayout.Label("VoxelObject Has No Builder!", boldMiddle);
                ClearEditor();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                return;
            }

            DrawToolLine(new VoxelTool[] {VoxelTool.Select});
            DrawCommandLine(new VoxelAction[] {VoxelAction.Clear, VoxelAction.Fill});
            DrawCommandLine(new VoxelAction[] {VoxelAction.Separate, VoxelAction.CopyUp});
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();


            // Transform
            EditorGUILayout.BeginVertical(_columnWidth);
            GUILayout.Label("Transform", boldMiddle);
            DrawToolLine(TransformTools);
            DrawToolLine(SizeTools);
            DrawTransformLocks(_columnWidth);
            DrawTransformApplyButtons(_columnWidth);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
            // Draw Tools
            EditorGUILayout.BeginVertical(_columnWidth);
            GUILayout.Label("Edit", boldMiddle);
            DrawToolLine(BasicEditTools);
            DrawToolLine(SecondaryTools);
            EditorGUILayout.EndVertical();

            /*
            // Palette
            EditorGUILayout.Space();
            GUILayout.Label("Palette", boldMiddle);
            DrawPalette(targetGameObject, targetVoxelObject);
            */
            if (position.height < position.width) EditorGUILayout.EndHorizontal();
        }

        void DrawTransformLocks(GUILayoutOption width)
        {
            EditorGUILayout.BeginHorizontal(width);

            GUILayout.Label("Lock Transform");
            GUI.backgroundColor = _targetVoxelObject.lockPosition ? Color.gray : Color.white;
            if (GUILayout.Button("P", _smallButtonWidth))
                _targetVoxelObject.lockPosition = !_targetVoxelObject.lockPosition;
            GUI.backgroundColor = _targetVoxelObject.lockRotation ? Color.gray : Color.white;
            if (GUILayout.Button("R", _smallButtonWidth))
                _targetVoxelObject.lockRotation = !_targetVoxelObject.lockRotation;
            GUI.backgroundColor = _targetVoxelObject.lockScale ? Color.gray : Color.white;
            if (GUILayout.Button("S", _smallButtonWidth))
                _targetVoxelObject.lockScale = !_targetVoxelObject.lockScale;

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        void DrawTransformApplyButtons(GUILayoutOption width)
        {
            EditorGUILayout.BeginHorizontal(width);

            GUI.enabled = _targetVoxelObject.lockRotation && _targetVoxelObject.lockScale;
            if (GUILayout.Button("Apply Scale & Rotation"))
            {
                RecordVoxelObjectForUndo(_targetVoxelObject, "Rotation Applyed to Map");
                _targetVoxelObject.ApplyScale();
                _targetVoxelObject.ApplyRotation();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        void DrawToolLine(VoxelTool[] tools)
        {
            if (tools ==null || tools.Length == 0)
            {
                return;
            }

            GUILayoutOption width = GUILayout.Width(fullW / tools.Length - separatorW);

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < tools.Length; i++)
            {
                DrawToolButton(tools[i], width);
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawToolButton(VoxelTool buttonTool, GUILayoutOption width)
        {
            GUI.backgroundColor = buttonTool == Tool ? Color.gray : Color.white;
            if (GUILayout.Button(buttonTool.ToString(), width))
            {
                if (Tool != buttonTool)
                {
                    Tools.hidden = true;
                    Tool = buttonTool;
                }
                else
                {
                    Tools.hidden = false;
                    Tool = VoxelTool.Non;
                }
            }

            GUI.backgroundColor = Color.white;
        }


        void DrawCommandLine(VoxelAction[] actions)
        {

            if ( actions ==null || actions.Length == 0)
            {
                return;
            }

            GUILayoutOption width = GUILayout.Width(fullW / actions.Length - separatorW);

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < actions.Length; i++)
            {
                GUI.enabled = IsActionEnabled(actions[i]);
                if (GUILayout.Button(actions[i].ToString(), width))
                {
                    DoVoxelAction(actions[i]);
                }
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        bool IsActionEnabled(object action)
        {
            switch (action)
            {
                case VoxelAction.Fill:
                case VoxelAction.Clear:
                    return true;
                case VoxelAction.CopyUp:
                    return _targetGameObject.transform.parent != null &&
                           _targetGameObject.transform.parent.GetComponentInParent<VoxelObject>() != null &&
                           _targetVoxelObject.lockPosition &&
                           _targetVoxelObject.lockRotation &&
                           _targetVoxelObject.lockScale;
                case VoxelAction.Separate:
                    return Tool == VoxelTool.Select;
            }

            return false;
        }

        void DoVoxelAction(VoxelAction action)
        {
            switch (action)
            {
                case VoxelAction.Clear:
                    if (Tool != VoxelTool.Select)
                    {
                        RecordVoxelObjectForUndo(_targetVoxelObject, "Map Cleared");
                        _targetVoxelObject.ClearWholeMap();
                    }
                    else
                    {
                        RecordVoxelObjectForUndo(_targetVoxelObject, "Selection Cleared");
                        _targetVoxelObject.Map.SetRange(_selectionMin, _selectionMax, VoxelMap.VoxelAreaAction.Clear,
                            ValueIndex);
                    }

                    break;
                case VoxelAction.Fill:
                    if (Tool != VoxelTool.Select)
                    {
                        RecordVoxelObjectForUndo(_targetVoxelObject, "Map Filled");
                        _targetVoxelObject.FillWholeMap(ValueIndex);
                    }
                    else
                    {
                        RecordVoxelObjectForUndo(_targetVoxelObject, "Selection Filled");
                        _targetVoxelObject.Map.SetRange(_selectionMin, _selectionMax, VoxelMap.VoxelAreaAction.Fill,
                            ValueIndex);
                    }

                    break;
                case VoxelAction.CopyUp:
                    CopyUp();
                    break;
                case VoxelAction.Separate:
                    Separate();
                    break;
            }
        }

        static void ClickTotTool(VoxelTool clickedTool)
        {
            if (Tool != clickedTool)
            {
                Tools.hidden = true;
                Tool = clickedTool;
            }
            else
            {
                Tools.hidden = false;
                Tool = VoxelTool.Non;
            }
        }

        void DrawPalette(GameObject go, VoxelObject vo)
        {
            if (!((IList)PaletteUsingTools).Contains(Tool))
            {
                GUI.enabled = false;
            }

            PaletteItem[] palette = vo.Builder.palette.GetPletteItems();

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < palette.Length; i++)
            {
                PaletteItem item = palette[i];
                GUI.backgroundColor = item.color;
                if (GUILayout.Button(ValueIndex == item.value ? "X" : ""))
                {
                    ValueIndex = item.value;
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }
    }
}
#endif