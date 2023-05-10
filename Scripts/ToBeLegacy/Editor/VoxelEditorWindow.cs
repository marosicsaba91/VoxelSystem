#if UNITY_EDITOR
using UnityEngine;
using UnityEditor; 

namespace VoxelSystem
{
	public partial class VoxelEditorWindow : EditorWindow
	{
		// ----------------------- STATE ------------------------

		static VoxelAction VoxelActon => _editorComponent.SelectedAction;
		static VoxelTool Tool => _editorComponent.SelectedTool;
		static int PaletteIndex => _editorComponent.SelectedPaletteIndex;

		static IVoxelEditor _editorComponent;

		void OnEnable()
		{
			titleContent = new("Voxel Editor");
			Undo.undoRedoPerformed += UndoRedoCalled;
			Selection.selectionChanged += ChangeTarget;
			SceneView.duringSceneGui += OnSceneGUI;
			ChangeTarget();
		}

		void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoRedoCalled;
			Selection.selectionChanged -= ChangeTarget;
			SceneView.duringSceneGui -= OnSceneGUI;
		}

		void ChangeTarget()
		{
			SetTargetObject();
			Repaint();
		}

		void SetTargetObject()
		{
			GameObject target = Selection.activeGameObject;					
			if (target == null) return;
			target.TryGetComponent(out _editorComponent); 
		}

		void UndoRedoCalled()
		{
			if (_editorComponent == null) return;

			_editorComponent.Map?.UndoRedoEvenInvokedOnMap();

			if (_editorComponent.transform.parent != null &&
				_editorComponent.transform.parent.TryGetComponent(out IVoxelEditor parent))
			{
				parent.Map?.UndoRedoEvenInvokedOnMap();
			}
		}

		void OnSceneGUI(SceneView scene)
		{
			Event guiEvent = Event.current;

			bool enableEdit =
				!Equals(_editorComponent, null) &&
				_editorComponent != null &&
				_editorComponent.EnableEdit &&
				!Equals(_editorComponent.Map, null) &&
				Tool != VoxelTool.None;

			Tools.hidden = enableEdit;

			if (!enableEdit)
				return;

			// ------------------------------------------------------

			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			VoxelToolHandler hander = Tool.GetHandler();
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			hander.ExecuteEditorControl(_editorComponent, guiEvent, ray);

		}
	}
}
#endif