/*
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
#endif

namespace VoxelSystem
{
public partial class VoxelObject 
{
    // normal mono stuff here

#if UNITY_EDITOR
    [Overlay(typeof(SceneView), title)]
    class QuickOverlay : ToolbarOverlay, ITransientOverlay
    {
        VoxelObject _target;

        void BurnToScreen() // some dummy ui
        {
            if (GUILayout.Button("Action"))
            {
            }

            GUILayout.Label("YO");
        }

        const string title = "Voxel Editor";

        [EditorToolbarElement(id, typeof(SceneView))]
        class ToggleExample : ToolbarToggle
        {
            const string id = "VoxelToolbar/Tool";
            VoxelTool _tool;
            public ToggleExample(VoxelTool tool)
            {
                _tool = tool;
                text = tool.ToString();
                this.RegisterValueChangedCallback(OnStateChange);
            }

            void OnStateChange(ChangeEvent<bool> evt)
            {
                if (evt.newValue)
                { 
                    //text = "Toggle ON";
                }
                else
                { 
                    //text = "Toggle OFF";
                }
            }
        }
        public override void OnCreated()
        {
            Selection.selectionChanged += Update;
            
            Update();
        }

        public override VisualElement CreatePanelContent()
        {
            VisualElement content = base.CreatePanelContent();

            content.Add(new ToggleExample(VoxelTool.Attach));
            content.Add(new ToggleExample(VoxelTool.Erase));
            
            return content;
        }

        public override void OnWillBeDestroyed()
        {
            Selection.selectionChanged -= Update;
            Update();
        }

        public bool visible => _isVisible;
        bool _isVisible = false;

        void Update()
        {
            if (Selection.activeGameObject == null) 
                _isVisible = false;
            else if (Selection.activeGameObject == (_target?.gameObject ? _target?.gameObject : null))
                _isVisible = true;
            else if (!Selection.activeGameObject.TryGetComponent(out _target)) _isVisible = (false);
                else _isVisible = true;
            
        }
    }
#endif
}
}
*/