# if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	[CustomEditor(typeof(VoxelTester))]
	partial class VoxelTesterEditor : Editor
	{
		VoxelTester GetTarget()
		{
			if (Selection.objects.Length != 1)
				return null;

			if (Selection.objects[0] is GameObject selectedGameObj)
				return selectedGameObj.GetComponent<VoxelTester>();

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
			 
			bool isMouseEvent = guiEvent.type is EventType.MouseMove or EventType.MouseDown or 
				EventType.MouseDrag or EventType.MouseUp && guiEvent.button == 0;

			if (!isMouseEvent) return;

			VoxelTester tester = GetTarget();

			UpdateEnableEdit(tester);

			if (!_enableEdit) return;


			// ------------------------------------------------------
			
			Vector2 mousePosition = guiEvent.mousePosition; 
			bool mouseDown = guiEvent.type == EventType.MouseDown;

			Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
			tester.Raycast(ray, mouseDown);

			SceneView.RepaintAll();

			if (mouseDown)
			{
				GUIUtility.hotControl = 0;
				guiEvent.Use();
			}

		}

		void UpdateEnableEdit(VoxelTester tester) => _enableEdit =
						!Equals(tester, null) &&
						tester != null &&
						tester.EnableTest;
	}
}

#endif