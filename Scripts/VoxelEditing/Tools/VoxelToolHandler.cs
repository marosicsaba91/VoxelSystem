using MUtility; 
using System.Collections.Generic; 
using UnityEngine; 

namespace VoxelSystem
{
	public struct VoxelHandelInfo
	{
		public Vector3 position;
		public GeneralDirection3D direction;
		public HandeleConeType coneType;
		public string text;
	}

	public abstract class VoxelToolHandler
	{
		protected static ArrayVoxelMap _originalMap = null; // Target Voxel Map before Mouse Down
		protected static Vector3 _originalTransformPosition = Vector3.zero;
		protected static BoundsInt _originalSelection;
		protected static Vector3Int _originalMapSize;
		protected static float _standardSpacing = 1;

		// Raycast Helper Variables
		protected static bool _isLastRayHit;
		protected static bool _isMouseDown;
		protected static VoxelHit _lastValidHit;
		protected static VoxelHit _mouseDownHit;
		protected static VoxelHit _lastHandledHit;

		// Handle Helper Variables
		public static Vector3 _clickPositionGlobal;
		protected static Vector3 _lastHandleVector;
		protected static GeneralDirection3D _handleDragDirection;
		protected static int _handleSteps = 0;

		protected static GUIStyle textStyle = new GUIStyle()
		{
			alignment = TextAnchor.MiddleCenter,
			fontSize = 15,
			normal = new GUIStyleState() { textColor = Color.white }
		};

		// --------------------- CONTROL ---------------------------

		internal void ExecuteEditorControl(IVoxelEditor voxelEditor, Event guiEvent, Ray ray)
		{
#if UNITY_EDITOR
			Matrix4x4 matrix4X4 = UnityEditor.Handles.matrix;
			UnityEditor.Handles.matrix = voxelEditor.transform.localToWorldMatrix;


			if (voxelEditor.ToolState == ToolState.None)
			{
				_originalSelection = voxelEditor.Selection;
				_originalMapSize = voxelEditor.Map.FullSize;
			}

			ExecutedHandles(voxelEditor, ray, out bool useEvent);
			if (!useEvent)
				TryExecuteRaycast(voxelEditor, guiEvent, ray);

			if (useEvent)
				UseMouseEvent(guiEvent);

			UnityEditor.Handles.matrix = matrix4X4;
#endif
		}
		// --------------------- Raycast ---------------------

		void TryExecuteRaycast(IVoxelEditor voxelEditor, Event guiEvent, Ray ray)
		{ 
#if UNITY_EDITOR
			if (!DoRaycastVoxelCursor(voxelEditor, out bool raycastOutside))
			{
				_isLastRayHit = false;
				return;
			}

			HandleRaycast(voxelEditor, ray, guiEvent, raycastOutside);
			if (guiEvent.type == EventType.Repaint)
			{
				if (_isLastRayHit)
				{
					// DRAW VOXEL CURSOR
					Color color = GetCursorColor(voxelEditor.SelectedAction);
					UnityEditor.Handles.color = new(color.r, color.g, color.b, color.a / 4f);
					UnityEditor.Handles.DrawWireCube((Vector3)_lastValidHit.voxelIndex + Vector3.one * 0.5f, Vector3.one);
					UnityEditor.Handles.DrawWireCube(_lastValidHit.hitWorldPosition, Vector3.one * 0.2f);

					UnityEditor.Handles.color = color;
					Drawable drawable = GetDrawableVoxelSide(_lastValidHit);
					drawable.DrawHandle();
				}
			}
#endif
		}
		Color GetCursorColor(VoxelAction selectedAction) => selectedAction switch
		{
			VoxelAction.Attach => new Color(0.4f,1,0.3f),
			VoxelAction.Erase => new Color(1, 0.2f, 0.1f),
			VoxelAction.Overwrite => new Color(0.2f, 0.7f, 1),
			VoxelAction.Repaint => new Color(1, 1, 1),
			_ => Color.white,
		};

		void HandleRaycast(IVoxelEditor voxelEditor, Ray ray, Event guiEvent, bool raycastOutside)
		{
			if (!guiEvent.isMouse || guiEvent.button is not 0) return;

			Transform transform = voxelEditor.transform;
			VoxelMap map = _isMouseDown ? _originalMap : voxelEditor.Map;
			_isLastRayHit = map.Raycast(ray, out VoxelHit hit, transform, raycastOutside);

			if (_isLastRayHit)
				_lastValidHit = hit;

			if (guiEvent.type == EventType.MouseDown)
			{
				if (guiEvent.modifiers == EventModifiers.Control)
					Debug.Log(hit.voxelIndex);

				_originalSelection = voxelEditor.Selection;
				voxelEditor.ToolState = ToolState.Down;
				HandleCursorDown(voxelEditor, _isLastRayHit, hit);
			}
			else if (!_isMouseDown)
			{
				guiEvent.Use();
				return;
			}

			if (guiEvent.type == EventType.MouseDrag)
			{
				voxelEditor.ToolState = ToolState.Drag;
				HandleCursorDrag(voxelEditor, _isLastRayHit, hit);
			}
			else if (guiEvent.type == EventType.MouseUp)
			{
				voxelEditor.ToolState = ToolState.Up;
				if (OnVoxelCursorUp(voxelEditor, _lastValidHit))
					voxelEditor.Map.MapChanged();
				_isMouseDown = false;
				voxelEditor.ToolState = ToolState.None;
				_originalSelection = voxelEditor.Selection;
			}

			UseMouseEvent(guiEvent);
		}

		void HandleCursorDown(IVoxelEditor voxelEditor, bool isHit, VoxelHit hit)
		{
			_mouseDownHit = hit;
			_isMouseDown = isHit;

			if (!isHit) return;

			if (_originalMap == null || _originalMap.FullSize == Vector3Int.zero)
				_originalMap = new ArrayVoxelMap();
			_originalMap.SetupFrom(voxelEditor.Map);

			if (OnVoxelCursorDown(voxelEditor, hit))
				voxelEditor.Map.MapChanged();

			_lastHandledHit = hit;
		}

		void HandleCursorDrag(IVoxelEditor voxelEditor, bool isHit, VoxelHit hit)
		{
			if (!isHit) return;
			if (hit.voxelIndex == _lastHandledHit.voxelIndex) return;

			if (OnVoxelCursorDrag(voxelEditor, hit))
				voxelEditor.Map.MapChanged();

			_lastHandledHit = hit;
		}


		// --------------------- Handles ---------------------------

		void ExecutedHandles(IVoxelEditor voxelEditor, Ray ray, out bool useEvent)
		{
			useEvent = false;
			foreach (VoxelHandelInfo handleInfo in GetHandeles(voxelEditor))
			{
				ExecuteOneHandle(voxelEditor, handleInfo, ray, out bool useE);
				useEvent |= useE;
			}
		}

		void ExecuteOneHandle(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, Ray ray, out bool useEvent)
		{
			useEvent = false;
#if UNITY_EDITOR
			VoxelMap map = voxelEditor.Map;
			VoxelTool _tool = voxelEditor.SelectedTool;
			GeneralDirection3D direction = handleInfo.direction;
			Axis3D axis = direction.GetAxis();
			Vector3 handlePos = handleInfo.position;
			Vector3Int editingSpace = voxelEditor.HasSelection() ? voxelEditor.Selection.size : map.FullSize;
			float sizeMultiplier = Mathf.Sqrt(editingSpace.AbsMean() / 20f);  // 20 looks good
			_standardSpacing = sizeMultiplier * 2;

			Color color = axis.GetAxisColor();
			Color focused = (color + Color.white) / 2f;
			UnityEditor.Handles.color = color;

			Vector3 directionVector = direction.ToVector();
			var rotation = Quaternion.LookRotation(directionVector);

			UnityEditor.Handles.CapFunction capFunction = handleInfo.coneType == HandeleConeType.Arrow
				? UnityEditor.Handles.ConeHandleCap
				: UnityEditor.Handles.CubeHandleCap;

			if (handleInfo.coneType == HandeleConeType.Box)
				sizeMultiplier *= 0.7f;

			HandleResult handleResult =
				AdvancedHandles.Handle(handlePos, rotation, sizeMultiplier, capFunction, color, focused, color);

			// Handle Click
			if (handleResult.handleEvent is HandleEvent.LmbClick or HandleEvent.LmbDoubleClick)
			{
				if (OnHandleClick(voxelEditor, handleInfo))
				{
					voxelEditor.Map.MapChanged();
					useEvent = true;
					return;
				}
			}

			// Handle Drag
			switch (handleResult.handleEvent)
			{
				case HandleEvent.LmbPress: // START DRAG 
					_originalSelection = voxelEditor.Selection;
					voxelEditor.ToolState = ToolState.Down;
					_lastHandleVector = Vector3Int.zero;
					_clickPositionGlobal = voxelEditor.transform.TransformPoint(handleResult.clickPosition);
					_originalMap ??= new ArrayVoxelMap();
					_originalMap.SetupFrom(map);
					_originalMapSize = map.FullSize;
					_originalTransformPosition = voxelEditor.transform.position;
					if (OnHandleDown(voxelEditor, handleInfo))
						voxelEditor.Map.MapChanged();
					break;

				case HandleEvent.LmbDrag: // DRAG

					_handleSteps = GetDistance(_clickPositionGlobal, ray, directionVector);

					voxelEditor.ToolState = ToolState.Drag;
					Vector3Int vec = direction.ToVectorInt() * _handleSteps;
					bool changed = _lastHandleVector != vec;
					_handleDragDirection = direction;
					_lastHandleVector = vec;
					if (changed)
						if (OnHandleDrag(voxelEditor, handleInfo, _handleSteps))
							voxelEditor.Map.MapChanged();
					break;

				case HandleEvent.LmbRelease: // RELEASE 
					voxelEditor.ToolState = ToolState.Up;
					_handleSteps = GetDistance(_clickPositionGlobal, ray, directionVector);
					if (OnHandleUp(voxelEditor, handleInfo, _handleSteps))
						voxelEditor.Map.MapChanged();
					_handleSteps = 0;
					_lastHandleVector = Vector3Int.zero;
					voxelEditor.ToolState = ToolState.None;
					_originalSelection = voxelEditor.Selection;

					break;
			}

			if (handleResult.handleEvent is HandleEvent.LmbPress or HandleEvent.LmbDrag or HandleEvent.LmbRelease)
				useEvent = true;

			if (!handleInfo.text.IsNullOrEmpty())
			{
				textStyle.normal.textColor = (color + Color.white) / 2f;
				UnityEditor.Handles.Label(handlePos + Vector3.up * _standardSpacing, handleInfo.text, textStyle);
			}

#endif
		}

		static int GetDistance(Vector3 startPoint, Ray ray, Vector3 directionVector)
		{
			Vector3 planeNormal = directionVector.GetPerpendicular(); 
			Plane plane = new(planeNormal, startPoint);
			plane.Raycast(ray, out float enter);
			Vector3 intersect = ray.GetPoint(enter);
			Vector3 cursorMovement = intersect - startPoint;
			float distance = Vector3.Dot(cursorMovement, directionVector); 
			return Mathf.RoundToInt(distance);
		}

		protected void Reset(IVoxelEditor voxelEditor)
		{
			voxelEditor.Map.SetupFrom(_originalMap);
			voxelEditor.transform.position = _originalTransformPosition;
			voxelEditor.Selection = _originalSelection;
		}

		// ------------------ Static Methods -----------------------------

		static void UseMouseEvent(Event guiEvent)
		{
			if (guiEvent.type is EventType.MouseDown or EventType.MouseDrag or EventType.MouseUp && guiEvent.button == 0) // Left Mouse
				guiEvent.Use();
		}

		protected static bool IsMapSideVisible(IVoxelEditor voxelEditor, Vector3 size, GeneralDirection3D side)
		{
			// TODO: Simplify

			Vector3 sideNormal = side.ToVectorInt();
			Vector3 halfSize = new(size.x / 2f, size.y / 2f, size.z / 2f);
			Vector3 halfNormalInSize = new(halfSize.x * sideNormal.x, halfSize.y * sideNormal.y, halfSize.z * sideNormal.z);
			Vector3 planeOrigin = halfSize + halfNormalInSize;

			// Transform points
			sideNormal = voxelEditor.transform.TransformDirection(sideNormal);
			planeOrigin = voxelEditor.transform.TransformPoint(planeOrigin);

			Camera cam = Camera.current;
			Vector3 cameraDir =
					cam.orthographic ?
					cam.transform.forward :
					planeOrigin - cam.transform.position;
			float angle = Vector3.Angle(cameraDir, sideNormal);

			const float epsilon = 0.1f;
			return angle > 90 - epsilon;
		}

		protected static void Translate(IVoxelEditor voxelEditor, GeneralDirection3D direction, int steps)
		{
			Vector3 localShift = direction.ToVector() * steps;
			Vector3 globalShift = voxelEditor.transform.TransformVector(localShift);
			voxelEditor.transform.position = _originalTransformPosition + globalShift;
		}

		protected static Vector3 GetMapSidePosition(IVoxelEditor voxelEditor, GeneralDirection3D direction)
		{
			Vector3 size;
			Vector3 center;
			if (voxelEditor.HasSelection())
			{
				size = voxelEditor.Selection.size;
				center = voxelEditor.Selection.center;

			}
			else
			{
				size = voxelEditor.Map.FullSize;
				center = size / 2f;
			}

			Vector3 dir = direction.ToVector();
			return center + (dir.MultiplyAllAxis(size) / 2f) + dir * _standardSpacing;
		}

		static Drawable GetDrawableVoxelSide(VoxelHit hit)
		{
			var polygons = new List<Vector3[]>
			{
				GetVoxelSide(hit.voxelIndex, hit.side, 1f),
				GetVoxelSide(hit.voxelIndex, hit.side, 0.75f),
				GetVoxelSide(hit.voxelIndex, hit.side, 0.5f),
				GetVoxelSide(hit.voxelIndex, hit.side, 0.25f)
			};
			return new Drawable(polygons);

			static Vector3[] GetVoxelSide(Vector3Int localCoordinate, GeneralDirection3D side, float sizeMultiplier)
			{
				Vector3 x =
					side is GeneralDirection3D.Up or GeneralDirection3D.Down ? new(sizeMultiplier * 0.5f, y: 0, z: 0) :
					side is GeneralDirection3D.Left or GeneralDirection3D.Right ? new(x: 0, y: 0, sizeMultiplier * 0.5f) :
					side is GeneralDirection3D.Forward or GeneralDirection3D.Back ? new(x: 0, sizeMultiplier * 0.5f, z: 0) : Vector3.zero;
				Vector3 y =
					side is GeneralDirection3D.Up or GeneralDirection3D.Down ? new(x: 0, y: 0, sizeMultiplier * 0.5f) :
					side is GeneralDirection3D.Left or GeneralDirection3D.Right ? new(x: 0, sizeMultiplier * 0.5f, z: 0) :
					side is GeneralDirection3D.Forward or GeneralDirection3D.Back ? new(sizeMultiplier * 0.5f, y: 0, z: 0) : Vector3.zero;
				Vector3 offset =
					side == GeneralDirection3D.Up ? new(x: 0, y: 0.5f, z: 0) :
					side == GeneralDirection3D.Down ? new(x: 0, y: -0.5f, z: 0) :
					side == GeneralDirection3D.Left ? new(x: -0.5f, y: 0, z: 0) :
					side == GeneralDirection3D.Right ? new(x: 0.5f, y: 0, z: 0) :
					side == GeneralDirection3D.Forward ? new(x: 0, y: 0, z: 0.5f) :
					side == GeneralDirection3D.Back ? new(x: 0, y: 0, z: -0.5f) : Vector3.zero;

				Vector3 halfSize = new(x: 0.5f, y: 0.5f, z: 0.5f);
				return new[]{
				localCoordinate + offset + x + y + halfSize,
				localCoordinate + offset + x + -y + halfSize,
				localCoordinate + offset + -x + -y + halfSize,
				localCoordinate + offset + -x + y + halfSize,
				localCoordinate + offset + x + y + halfSize,
			};
			}
		}


		// ------------------ Supported Actions -----------------------------

		protected static readonly VoxelAction[] noVoxelAction = new VoxelAction[0];
		protected static readonly VoxelAction[] allVoxelActions = VoxelEditor_EnumHelper._allVoxelActions;
		public virtual VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => noVoxelAction;

		protected static VoxelAction[] GetTransformActions(IVoxelEditor voxelEditor) => 
			voxelEditor.HasSelection() ? VoxelEditor_EnumHelper._transformActions : noVoxelAction;

		// ------------------ Virtual Methods: Cursor Voxel -----------------------------


		protected virtual bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = false;
			return false;
		}
		protected virtual bool OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit) => false;
		protected virtual bool OnVoxelCursorDrag(IVoxelEditor voxelEditor, VoxelHit hit) => false;
		protected virtual bool OnVoxelCursorUp(IVoxelEditor voxelEditor, VoxelHit hit) => false;

		// ------------------ Virtual Methods: Handles -----------------------------

		protected virtual IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{
			yield break;
		}

		protected virtual bool OnHandleClick(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo) => false;
		protected virtual bool OnHandleDown(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo) => false;
		protected virtual bool OnHandleDrag(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps) => false;
		protected virtual bool OnHandleUp(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps) => false;
	}
}
