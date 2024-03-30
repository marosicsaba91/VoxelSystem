#if UNITY_EDITOR

using EasyEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoxelSystem
{
	public class VoxelEditorWindow : EditorWindow
	{
		[MenuItem("Window/" + EditorConstants.categoryPath + "Voxel Editor Window")]
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

		Vector2 scrollPos = Vector2.zero;

		void DrawEditor(VoxelEditor voxelEditor)
		{
			if (voxelEditor == null) return;

			SerializedObject serializedObject = new(voxelEditor);
			VoxelEditorGUI.SetupGuiContentAndStyle();
			if (voxelEditor.Map == null)
			{
				EditorGUILayout.HelpBox("No Map to Edit. Use Voxel Editor & Voxel Object Component!", UnityEditor.MessageType.Warning);
				return;
			}

			Rect windowRect = position;
			windowRect.position = Vector2.zero;
			windowRect.x = EditorGUIUtility.standardVerticalSpacing;
			windowRect.y = EditorGUIUtility.standardVerticalSpacing;
			windowRect.width -= 2 * EditorGUIUtility.standardVerticalSpacing;
			windowRect.height -= 2 * EditorGUIUtility.standardVerticalSpacing;
			
			const float minWith = 250f;
			const float scrollbarWidth = 14f;

			Rect contentRect = windowRect;
			contentRect.height += 100;

			VoxelShapeBuilder selectedShape = voxelEditor.SelectedShape;
			IReadOnlyList<ExtraVoxelControl> extraControls = selectedShape == null ? null : selectedShape.GetExtraControls();
			int extraControlCount = extraControls == null ? 0: extraControls.Count;

			float contentHeight = 490f
				+ VoxelEditorGUI.GetPaletteHeight(voxelEditor.MaterialPalette.Count)
				+ VoxelEditorGUI.GetPaletteHeight(voxelEditor.ShapePalette.ItemCount)
				+ EditorHelper.GetStandardPanelHeight(extraControlCount);

			float verticalScrollbarWidth = contentHeight > windowRect.height ? scrollbarWidth : 0;
			contentRect.height = contentHeight;
			contentRect.width = Mathf.Max(contentRect.width - verticalScrollbarWidth, minWith);


			scrollPos = GUI.BeginScrollView(windowRect, scrollPos, contentRect);

			VoxelEditorGUI.DrawHeader(voxelEditor, ref contentRect);
			VoxelEditorGUI.DrawMapActions(voxelEditor, ref contentRect);

			contentRect.y += 6;
			contentRect.height -= 6;
			VoxelEditorGUI.DrawControlPanel(voxelEditor, ref contentRect);
			contentRect.y += 6;
			contentRect.height -= 6;
			VoxelEditorGUI.DrawPalettes(voxelEditor, ref contentRect);			
			VoxelEditorGUI.DrawExtraControls(voxelEditor, ref contentRect);			
			VoxelEditorGUI.DrawVoxelPreview(voxelEditor, ref contentRect, GeneralDirection2D.Up);

			EditorGUILayout.GetControlRect(false, contentRect.y);
			GUI.EndScrollView();

			serializedObject.ApplyModifiedProperties();
		}

	}
}

#endif