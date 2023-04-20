#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace VoxelSystem.Editor
{
    [CustomEditor(typeof(BlockLibrary))]
    public class BlockLibraryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            
            base.OnInspectorGUI();
        }

        void OnSceneGUI()
        { 
        }
    }
}
#endif