#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal.VR;
using UnityEngine;

namespace VoxelSystem
{
	partial class VoxelEditor_CustomEditor : Editor
	{
		static string _lastUndoGroupName = string.Empty;
		static VoxelEditor_CustomEditor()
		{
			Undo.undoRedoPerformed += UndoRedoCalled;
			Undo.postprocessModifications += PostProcessModifications;
		}

		private static UndoPropertyModification[] PostProcessModifications(UndoPropertyModification[] modifications)
		{
			_lastUndoGroupName = Undo.GetCurrentGroupName();
			return modifications;
		}


		static void UndoRedoCalled()
		{
			string groupName = _lastUndoGroupName;
			_lastUndoGroupName = Undo.GetCurrentGroupName();

			Debug.Log($"UndoRedoCalled: {groupName}");
			int index = groupName.IndexOf(VoxelMap.undoGuidString);
			if (index < 0) return;

			string guidString = groupName[(index + VoxelMap.undoGuidString.Length)..];
			if (VoxelMap.TryGetMapByGuid(guidString, out VoxelMap map))
				map.UndoRedoEvenInvokedOnMap();
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

		void OnSceneGUI()
		{
			Event guiEvent = Event.current;

			VoxelEditor editor = GetTarget();

			bool enableEdit =
				!Equals(editor, null) &&
				editor != null &&
				editor.EnableEdit &&
				!Equals(editor.Map, null);


			Tools.hidden = enableEdit;

			if (!enableEdit)
				return;

			// ------------------------------------------------------

			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			VoxelToolHandler hander = editor.selectedTool.GetHandler();
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			hander?.ExecuteEditorControl(editor, guiEvent, ray);

		}

	}
}

#endif