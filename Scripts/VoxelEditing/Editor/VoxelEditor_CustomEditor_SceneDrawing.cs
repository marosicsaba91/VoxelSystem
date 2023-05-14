#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
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

		bool _enableEdit;

		void OnDisable()
		{
			Tools.hidden = false;
		}

		void OnSceneGUI()
		{
			Event guiEvent = Event.current;

			VoxelEditor editor = GetTarget();

			UpdateEnableEdit(editor);

			Tools.hidden = _enableEdit && editor.SelectedTool != VoxelTool.None;

			if (!_enableEdit)
				return;

			// ------------------------------------------------------

			if (editor.SelectedTool == VoxelTool.None) return;

			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			VoxelToolHandler hander = editor.SelectedTool.GetHandler();
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			hander?.ExecuteEditorControl(editor, guiEvent, ray);

		}

		void UpdateEnableEdit(VoxelEditor editor) => _enableEdit =
						!Equals(editor, null) &&
						editor != null &&
						editor.EnableEdit &&
						!Equals(editor.Map, null);
	}
}

#endif