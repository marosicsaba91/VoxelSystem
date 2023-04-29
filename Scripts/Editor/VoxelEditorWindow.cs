#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using MUtility;

namespace VoxelSystem
{
	public partial class VoxelEditorWindow : EditorWindow
	{
		// ----------------------- STATE ------------------------

		static VoxelAction SelectedVoxelAction 
		{
			get=> _editorComponent.SelectedAction; 
			set=> _editorComponent.SelectedAction = value;
		}
		static VoxelTool SelectedTool
		{
			get => _editorComponent.SelectedTool;
			set => _editorComponent.SelectedTool = value;
		}

		public static int SelectedPaletteIndex
		{
			get => _editorComponent.SelectedPaletteIndex;
			set => _editorComponent.SelectedPaletteIndex = value;
		}

		static GameObject _targetGameObject;
		static IVoxelEditable _editorComponent;

		void OnEnable()
		{
			titleContent = new("Voxel Editor");
			Undo.undoRedoPerformed += UndoRedoCalled;
			Selection.selectionChanged += ChangeTarget;
			SceneView.duringSceneGui += OnSceneGUI;
			_originalMap = null;
			ChangeTarget();

			//SceneView.over
		}

		void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoRedoCalled;
			Selection.selectionChanged -= ChangeTarget;
			SceneView.duringSceneGui -= OnSceneGUI;
			ClearEditor();
		}

		void ClearEditor()
		{
			_originalMap = null;
		}

		void ChangeTarget()
		{
			SetTargetObject();
			Repaint();
		}

		void SetTargetObject()
		{
			_targetGameObject = Selection.activeGameObject;
			_editorComponent = null;
			if (_targetGameObject == null)
				return;
			ResetSelection();
			_editorComponent = _targetGameObject.GetComponent<IVoxelEditable>();
		}

		public static void RecordVoxelObjectForUndo(IVoxelEditable editor, string message, params Object[] otherObjects)
		{ 
			Object[] objects = new Object[] { editor.RecordableUnityObject, editor.transform };
			if (!otherObjects.IsNullOrEmpty())
				objects.Concat(otherObjects);
			Undo.RecordObjects(objects, message);
			if (editor.RecordableUnityObject is ScriptableObject)
				EditorUtility.SetDirty(editor.RecordableUnityObject);
		}

		void UndoRedoCalled()
		{
			if (_editorComponent == null) return;

			_editorComponent.Map?.UndoRedoEvenInvokedOnMap();

			if (_editorComponent.transform.parent != null &&
				_editorComponent.transform.parent.TryGetComponent(out IVoxelEditable parent))
			{
				parent.Map?.UndoRedoEvenInvokedOnMap();
			}
		}

		static bool IsMapSideSeen(GeneralDirection3D side)
		{
			if (_editorComponent == null)
			{ return false; }

			Vector3 sideNormal = side.ToVectorInt();
			Vector3Int size = _editorComponent.Map.FullSize;
			Vector3 halfSize = new(size.x / 2f, size.y / 2f, size.z / 2f);
			Vector3 halfNormalInSize = new(halfSize.x * sideNormal.x, halfSize.y * sideNormal.y, halfSize.z * sideNormal.z);
			Vector3 planeOrigin = halfSize + halfNormalInSize;

			// Transform points
			sideNormal = _editorComponent.transform.TransformDirection(sideNormal);
			planeOrigin = _editorComponent.transform.TransformPoint(planeOrigin);

			Camera cam = Camera.current;
			Vector3 cameraDir =
					cam.orthographic ?
					cam.transform.forward :
					planeOrigin - cam.transform.position;
			float angle = Vector3.Angle(cameraDir, sideNormal);

			const float epsilon = 0.1f;
			return angle > 90 - epsilon;
		}
	}
}
#endif