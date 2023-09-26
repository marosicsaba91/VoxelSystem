#if UNITY_EDITOR

using MUtility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoxelSystem
{
	public class VoxelEditorWindow : EditorWindow
	{
		[MenuItem("Window/" + EditorConstants.categoryPath + "MyEditorWindow")]
		public static void ShowExample()
		{
			VoxelEditorWindow ew = GetWindow<VoxelEditorWindow>();
			ew.titleContent = new GUIContent("Voxel Editor Window");
		}

		// Use old System for Drawing GUI

		public void OnGUI()
		{
			foreach (Object item in Selection.objects)
			{
				if (item is GameObject go && go.TryGetComponent(out VoxelEditor voxelEditor))
				{
					DrawEditor(voxelEditor);
					return;
				}
			}

			EditorGUILayout.Space(position.height / 2 - 30);
			GUIStyle headerStyle = new(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
			GUIStyle paletteDarkStyle = new(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
			EditorGUILayout.LabelField("No Voxel Editor Selected", headerStyle);
			EditorGUILayout.LabelField("Select a GameObject with a VoxelEditor component", paletteDarkStyle);
		}

		void DrawEditor(VoxelEditor voxelEditor)
		{
			if (voxelEditor == null) return;

			SerializedObject serializedObject = new(voxelEditor);
			VoxelEditorGUI.SetupGuiContentAndStyle();
			if (voxelEditor.Map == null)
			{
				EditorGUILayout.HelpBox("No Map to Edit. Use VoxelFilter", UnityEditor.MessageType.Warning);
				return;
			}

			Rect rect = position;
			rect.position = Vector2.zero;
			rect.x = EditorGUIUtility.standardVerticalSpacing;
			rect.y = EditorGUIUtility.standardVerticalSpacing;
			rect.width -= 2 * EditorGUIUtility.standardVerticalSpacing;
			rect.height -= 2 * EditorGUIUtility.standardVerticalSpacing;

			VoxelEditorGUI.DrawHeader(voxelEditor, ref rect);
			VoxelEditorGUI.DrawMapActions(voxelEditor, ref rect);
			VoxelEditorGUI.DrawControlPanel(voxelEditor, ref rect);
			VoxelEditorGUI.DrawPalettes(voxelEditor, ref rect);
			
			VoxelEditorGUI.DrawVoxelPreview(voxelEditor, ref rect, GeneralDirection2D.Down);
			VoxelEditorGUI.DrawVoxelTransformation(voxelEditor, ref rect, GeneralDirection2D.Down);

			EditorGUILayout.GetControlRect(false, rect.y);

			serializedObject.ApplyModifiedProperties();
		}

	}
}

#endif