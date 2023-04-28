#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using MUtility;

namespace VoxelSystem
{
	// Need Refactor

	// VoxelEditor.Action: { Set, Fill, Clear, Repaint, Select}
	// VoxelEditor.Tool: { Move, Turn, Mirror, Resize, Repeat, Rescale, FloodFill, Picker }
	// VoxelEditor.Command: { Clear, Fill, Separate, CopyUp }

	enum VoxelTool { Non, Fill, Clear, Repaint, Face, Select, Move, Turn, Mirror, Resize, Repeat, Rescale, FloodFill, Picker }

	enum VoxelCommand { Clear, Fill, Separate, CopyUp }

	public partial class VoxelEditorWindow : EditorWindow
	{
		// ----------------------- STATE ------------------------

		static readonly VoxelTool[] _handleTools = { VoxelTool.Move, VoxelTool.Turn, VoxelTool.Mirror, VoxelTool.Resize, VoxelTool.Repeat, VoxelTool.Rescale };
		static readonly VoxelTool[] _cursorTools = { VoxelTool.Select, VoxelTool.Fill, VoxelTool.Clear, VoxelTool.Repaint, VoxelTool.Face, VoxelTool.FloodFill, VoxelTool.Picker };
		static readonly VoxelTool[] _paletteUsingTools = { VoxelTool.Fill, VoxelTool.Repaint, VoxelTool.Face, VoxelTool.FloodFill };

		static readonly VoxelTool[] _transformTools = { VoxelTool.Move, VoxelTool.Turn, VoxelTool.Mirror };
		static readonly VoxelTool[] _sizeTools = { VoxelTool.Resize, VoxelTool.Repeat, VoxelTool.Rescale };

		static readonly VoxelTool[] _basicEditTools = { VoxelTool.Fill, VoxelTool.Clear, VoxelTool.Repaint };
		static readonly VoxelTool[] _secondaryTools = { VoxelTool.Face, VoxelTool.FloodFill, VoxelTool.Picker };

		static VoxelMap.SetAction ToolToAreaAction() => Tool switch
		{
			VoxelTool.Repaint => VoxelMap.SetAction.Repaint,
			VoxelTool.Fill => VoxelMap.SetAction.Fill,
			VoxelTool.Clear => VoxelMap.SetAction.Clear,
			_ => VoxelMap.SetAction.Repaint
		};

		static VoxelTool Tool { get; set; } = VoxelTool.Non;
		public static int SelectedPaletteIndex { get; private set; } = 0;

		static GameObject _targetGameObject;
		static IVoxelEditable _targetVoxelObject;

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
			Tool = VoxelTool.Non;
			SelectedPaletteIndex = 0;
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
			_targetVoxelObject = null;
			if (_targetGameObject == null)
				return;
			ResetSelection();
			_targetVoxelObject = _targetGameObject.GetComponent<IVoxelEditable>();
		}

		public static void RecordVoxelObjectForUndo(IVoxelEditable vo, string message, params Object[] otherObjects)
		{
			if (vo.HasConnectedMap())
			{
				Object[] objects = new Object[] { vo.SharedMap, vo.transform };
				if (!otherObjects.IsNullOrEmpty())
					objects.Concat(otherObjects);
				Undo.RecordObjects(objects, message);
				EditorUtility.SetDirty(vo.SharedMap);
			}
			else
			{
				Object[] objects = new Object[] { vo.Object, vo.transform };
				if (!otherObjects.IsNullOrEmpty())
					objects.Concat(otherObjects);
				Undo.RecordObjects(objects, message);
			}
		}

		void UndoRedoCalled()
		{
			if (_targetVoxelObject != null)
			{
				_targetVoxelObject.Map?.UndoRedoEvenInvokedOnMap();
			}
			if (_targetVoxelObject != null &&
				_targetVoxelObject.transform != null &&
				_targetVoxelObject.transform.parent != null &&
				_targetVoxelObject.transform.parent.TryGetComponent(out IVoxelEditable parent))
			{
				parent.Map?.UndoRedoEvenInvokedOnMap();
			}
		}

		static bool IsMapSideSeen(GeneralDirection3D side)
		{
			if (_targetVoxelObject == null)
			{ return false; }

			Vector3 sideNormal = side.ToVectorInt();
			Vector3Int size = _targetVoxelObject.Map.FullSize;
			Vector3 halfSize = new(size.x / 2f, size.y / 2f, size.z / 2f);
			Vector3 halfNormalInSize = new(halfSize.x * sideNormal.x, halfSize.y * sideNormal.y, halfSize.z * sideNormal.z);
			Vector3 planeOrigin = halfSize + halfNormalInSize;

			// Transform points
			sideNormal = _targetVoxelObject.transform.TransformDirection(sideNormal);
			planeOrigin = _targetVoxelObject.transform.TransformPoint(planeOrigin);

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