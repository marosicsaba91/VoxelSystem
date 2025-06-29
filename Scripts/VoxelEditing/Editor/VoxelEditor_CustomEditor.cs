#if UNITY_EDITOR
using EasyEditor;
using MUtility;
using UnityEditor;
using UnityEngine;


namespace VoxelSystem
{
	[CustomEditor(typeof(VoxelEditor))]
	partial class VoxelEditor_CustomEditor : Editor
	{
		static VoxelEditor_CustomEditor()
		{
			Undo.undoRedoPerformed += UndoRedoCalled;
		}

		static void UndoRedoCalled()
		{
			foreach (Object obj in Selection.objects)
			{
				if (obj is not GameObject go) continue;
				if (!go.TryGetComponent(out VoxelEditor editor)) continue;
				if (editor.MapContainer == null) continue;
				editor.Map?.UndoRedoEvenInvokedOnMap();
				return;
			}
		}

		// ------------------------------------------------------

		VoxelEditor GetTarget()
		{
			// Normal target getter not working properly

			if (Selection.objects.Length != 1)
				return null;

			if (Selection.objects[0] is GameObject selectedGameObj)
			{
				return selectedGameObj.GetComponent<VoxelEditor>();
			}
			return null;

		}

		static bool _enableEdit;

		void OnDisable()
		{
			Tools.hidden = false;
		}

		void OnSceneGUI()
		{
			Event guiEvent = Event.current;

			VoxelEditor editor = GetTarget();

			_enableEdit = editor.IsEditingEnabled();

			Tools.hidden = _enableEdit && editor.SelectedTool != VoxelTool.None;

			if (!_enableEdit)
				return;

			if (guiEvent.isKey)
				HandleFastKeys(editor, guiEvent);

			// ------------------------------------------------------

			if (editor.SelectedTool == VoxelTool.None) return;

			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			VoxelToolHandler hander = editor.SelectedTool.GetHandler();
			Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
			hander?.ExecuteEditorControl(editor, guiEvent, ray);
		}

		void HandleFastKeys(VoxelEditor editor, Event guiEvent)
		{
			if (guiEvent.control && guiEvent.keyCode.TryGetValue(out int number))
			{
				editor.SelectedMaterialIndex = (byte)number;
				guiEvent.Use();
			}
			else if (guiEvent.keyCode.TryGetValue(out number))
			{
				editor.SelectedShapeId = number;
				guiEvent.Use();
			}


		}


		// ------------Inspector----------------

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DrawEditor(GetTarget(), serializedObject);
		}

		public static void DrawEditor(VoxelEditor voxelEditor, SerializedObject serializedObject)
		{
			if (voxelEditor == null) return;

			VoxelEditorGUI.SetupGuiContentAndStyle();

			if (voxelEditor.Map == null)
			{
				EditorGUILayout.HelpBox("No Map to Edit. Use Voxel Editor & Voxel Object Component!", UnityEditor.MessageType.Warning);
				return;
			}

			const float margin = 4;
			const float startY = 28;
			Rect contentRect = new(margin, startY, EditorHelper.FullWith - 2 * margin, 0);

			VoxelEditorGUI.DrawHeader(voxelEditor, ref contentRect);
			VoxelEditorGUI.DrawMapActions(voxelEditor, ref contentRect);

			bool isWindowOpen = EditorWindow.HasOpenInstances<VoxelEditorWindow>();


			if (!isWindowOpen)
			{
				if (GUI.Button(contentRect.SliceOut(24), "Open Editor Window"))
					VoxelEditorWindow.ShowExample();

				contentRect.RemoveOneSpace();

				VoxelEditorGUI.DrawControlPanel(voxelEditor, ref contentRect);
				VoxelEditorGUI.DrawPalettes(voxelEditor, ref contentRect);

				VoxelEditorGUI.DrawCubicTransformation(voxelEditor, ref contentRect);
				VoxelEditorGUI.DrawExtraControls(voxelEditor, ref contentRect);

				VoxelEditorGUI.DrawVoxelPreview(voxelEditor, ref contentRect, Side.Up);

				EditorGUILayout.GetControlRect(false, 150); // ???
			}


			EditorGUILayout.GetControlRect(false, contentRect.y - startY);

			serializedObject.ApplyModifiedProperties();
		}
	}
}

#endif