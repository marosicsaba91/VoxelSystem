using EasyEditor;
using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public struct VoxelHandelInfo
	{
		public Vector3 position;
		public GeneralDirection3D direction;
		public GeneralDirection3D side;
		public HandeleConeType coneType;
		public string text;
	}

	public abstract class VoxelToolHandler
	{
		protected static ArrayVoxelMap originalMap = null; // Target Voxel Map before Mouse Down
		protected static Vector3 originalTransformPosition = Vector3.zero;
		protected static BoundsInt originalSelection;
		protected static Vector3Int originalMapSize;
		protected static float standardSpacing = 1;

		// Raycast Helper Variables
		protected static bool isLastRayHit;
		protected static bool isMouseDown;
		protected static VoxelHit lastValidHit;
		protected static VoxelHit mouseDownHit;
		protected static VoxelHit lastHandledHit;
		protected static Color cursorColor = Color.white;
		protected static Ray globalRay;

		// Handle Helper Variables
		public static Vector3 clickPositionGlobal;
		protected static Vector3Int lastHandleVector;
		protected static GeneralDirection3D handleDragDirection;
		protected static int handleSteps = 0;

		protected static GUIStyle textStyle = new()
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
			UnityEditor.Handles.matrix = voxelEditor.Transform.localToWorldMatrix;
			globalRay = ray;

			if (voxelEditor.ToolState == ToolState.None)
			{
				originalSelection = voxelEditor.Selection;
				originalMapSize = voxelEditor.Map.FullSize;
			}

			ExecutedHandles(voxelEditor, ray, out bool useEvent);
			if (!useEvent)
				TryExecuteRaycast(voxelEditor, guiEvent, ray);

			else if (guiEvent.type is EventType.MouseDown or EventType.MouseDrag or EventType.MouseUp && guiEvent.button == 0) // Left Mouse
				guiEvent.Use();

			UnityEditor.Handles.matrix = matrix4X4;
#endif
		}

		// In the future, this method will work not just for handle drawing
		public static void Draw(WireShape d, Color c) => d.DrawHandle(c);

		// --------------------- Raycast ---------------------

		void TryExecuteRaycast(IVoxelEditor voxelEditor, Event guiEvent, Ray ray)
		{
#if UNITY_EDITOR
			if (!DoRaycastVoxelCursor(voxelEditor, out bool raycastOutside))
			{
				isLastRayHit = false;
				return;
			}

			HandleRaycast(voxelEditor, ray, guiEvent, raycastOutside);

			if (guiEvent.type == EventType.Repaint)
			{
				if (isLastRayHit)
				{
					Color color = GetActionColor(voxelEditor.SelectedAction);
					OnDrawCursor(voxelEditor, color, lastValidHit);
				}
			}
#endif
		}

		protected virtual void OnDrawCursor(IVoxelEditor voxelEditor, Color actionColor, VoxelHit hit)
		{
			Color fadedColor = new(actionColor.r, actionColor.g, actionColor.b, actionColor.a / 4f);
			Vector3 voxelCenter = (Vector3)hit.voxelIndex + Vector3.one * 0.5f;
			WireShape cube = new Cuboid(Vector3.one).ToDrawable();
			cube.Translate(voxelCenter);
			Draw(cube, fadedColor);

			WireShape side = GetDrawableVoxelSide(hit);
			Draw(side, actionColor);
		}

		Color GetActionColor(VoxelAction selectedAction) => selectedAction switch
		{
			VoxelAction.Attach => new Color(0.4f, 1, 0.3f),
			VoxelAction.Erase => new Color(1, 0.2f, 0.1f),
			VoxelAction.Overwrite => new Color(0.2f, 0.7f, 1),
			VoxelAction.Repaint => new Color(1, 1, 1),
			_ => Color.white,
		};

		void HandleRaycast(IVoxelEditor voxelEditor, Ray ray, Event guiEvent, bool raycastOutside)
		{
			if (!guiEvent.isMouse || guiEvent.button is not 0) return;

			Transform transform = voxelEditor.Transform;
			VoxelMap map = isMouseDown ? originalMap : voxelEditor.Map;
			isLastRayHit = map.Raycast(ray, out VoxelHit hit, transform, raycastOutside);

			if (isLastRayHit)
				lastValidHit = hit;

			if (guiEvent.type == EventType.MouseDown)
			{
				if (guiEvent.modifiers == EventModifiers.Control)
					Debug.Log(hit.voxelIndex);

				originalSelection = voxelEditor.Selection;
				voxelEditor.ToolState = ToolState.Down;
				HandleCursorDown(voxelEditor, isLastRayHit, hit);
			}
			else if (!isMouseDown)
			{
				guiEvent.Use();
				return;
			}

			if (guiEvent.type == EventType.MouseDrag)
			{
				voxelEditor.ToolState = ToolState.Drag;
				HandleCursorDrag(voxelEditor, isLastRayHit, hit);
			}
			else if (guiEvent.type == EventType.MouseUp)
			{
				voxelEditor.ToolState = ToolState.Up;
				MapChange change = OnVoxelCursorUp(voxelEditor, lastValidHit);
				voxelEditor.Map.MapChanged(change);

				isMouseDown = false;
				voxelEditor.ToolState = ToolState.None;
				originalSelection = voxelEditor.Selection;
			}
			guiEvent.Use();
		}

		void HandleCursorDown(IVoxelEditor voxelEditor, bool isHit, VoxelHit hit)
		{
			mouseDownHit = hit;
			isMouseDown = isHit;

			if (!isHit) return;

			if (originalMap == null || originalMap.FullSize == Vector3Int.zero)
				originalMap = new ArrayVoxelMap();
			originalMap.SetupFrom(voxelEditor.Map);

			MapChange change = OnVoxelCursorDown(voxelEditor, hit);
			voxelEditor.Map.MapChanged(change);

			lastHandledHit = hit;
		}

		void HandleCursorDrag(IVoxelEditor voxelEditor, bool isHit, VoxelHit hit)
		{
			if (!isHit) return;
			if (hit.voxelIndex == lastHandledHit.voxelIndex) return;

			MapChange change = OnVoxelCursorDrag(voxelEditor, hit);
			voxelEditor.Map.MapChanged(change);

			lastHandledHit = hit;
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
			standardSpacing = sizeMultiplier * 2;

			Color color = axis.GetAxisColor();
			Color focused = (color + Color.white) / 2f;
			UnityEditor.Handles.color = color;

			Vector3 directionVector = direction.ToVector();
			Quaternion rotation = Quaternion.LookRotation(directionVector);

			UnityEditor.Handles.CapFunction capFunction = handleInfo.coneType == HandeleConeType.Arrow
				? UnityEditor.Handles.ConeHandleCap
				: UnityEditor.Handles.CubeHandleCap;

			if (handleInfo.coneType == HandeleConeType.Box)
				sizeMultiplier *= 0.7f;

			HandleResult handleResult =
				AdvancedHandles.Handle(handlePos, rotation, sizeMultiplier, capFunction, color, focused, color);

			MapChange change;

			// Handle Click
			if (handleResult.handleEvent is HandleEvent.LmbClick or HandleEvent.LmbDoubleClick)
			{
				change = OnHandleClick(voxelEditor, handleInfo);
				if (change!= MapChange.None)
				{
					voxelEditor.ToolState = ToolState.Up;
					voxelEditor.Map.MapChanged(change);
					useEvent = true;
					return;
				}
			}
			// Handle Drag
			switch (handleResult.handleEvent)
			{
				case HandleEvent.LmbPress: // START DRAG 
					originalSelection = voxelEditor.Selection;
					voxelEditor.ToolState = ToolState.Down;
					lastHandleVector = Vector3Int.zero;
					clickPositionGlobal = voxelEditor.Transform.TransformPoint(handleResult.clickPosition);
					originalMap ??= new ArrayVoxelMap();
					originalMap.SetupFrom(map);
					originalMapSize = map.FullSize;
					originalTransformPosition = voxelEditor.Transform.position;

					change = OnHandleDown(voxelEditor, handleInfo);
					voxelEditor.Map.MapChanged(change);

					break;

				case HandleEvent.LmbDrag: // DRAG

					handleSteps = GetDistance(voxelEditor, clickPositionGlobal, ray, directionVector);

					voxelEditor.ToolState = ToolState.Drag;
					Vector3Int vec = direction.ToVectorInt() * handleSteps;
					bool changed = lastHandleVector != vec;
					handleDragDirection = direction;
					lastHandleVector = vec;
					if (changed)
					{
						change = OnHandleDrag(voxelEditor, handleInfo, handleSteps);
						voxelEditor.Map.MapChanged(change);
					}

					break;

				case HandleEvent.LmbRelease: // RELEASE 
					voxelEditor.ToolState = ToolState.Up;
					handleSteps = GetDistance(voxelEditor, clickPositionGlobal, ray, directionVector);
					change = OnHandleUp(voxelEditor, handleInfo, handleSteps);
					voxelEditor.Map.MapChanged(change);
					handleSteps = 0;
					lastHandleVector = Vector3Int.zero;
					voxelEditor.ToolState = ToolState.None;
					originalSelection = voxelEditor.Selection;

					break;
			}

			if (handleResult.handleEvent is HandleEvent.LmbPress or HandleEvent.LmbDrag or HandleEvent.LmbRelease)
				useEvent = true;

			if (!handleInfo.text.IsNullOrEmpty())
			{
				textStyle.normal.textColor = (color + Color.white) / 2f;
				UnityEditor.Handles.Label(handlePos + Vector3.up * standardSpacing, handleInfo.text, textStyle);
			}

#endif
		}

		static int GetDistance(IVoxelEditor editor, Vector3 startPoint, Ray ray, Vector3 directionVector)
		{
			Vector3 normal = directionVector;

			Ray localRay = globalRay.Transform(editor.Transform.worldToLocalMatrix);
			Vector3 paneRight = Vector3.Cross(localRay.direction, normal);
			Vector3 paneNormal = Vector3.Cross(paneRight, normal);
			Plain plane = new(startPoint, paneNormal);

			Vector3 intersect = plane.Intersect(ray);
			Vector3 cursorMovement = intersect - startPoint;
			float distance = Vector3.Dot(cursorMovement, directionVector);
			return Mathf.RoundToInt(distance);
		}

		protected void Reset(IVoxelEditor voxelEditor)
		{
			voxelEditor.Map.SetupFrom(originalMap);
			voxelEditor.Transform.position = originalTransformPosition;
			voxelEditor.Selection = originalSelection;
		}

		// ------------------ Static Methods -----------------------------

		protected static bool IsMapSideVisible(IVoxelEditor voxelEditor,  GeneralDirection3D side, float angleAllowed = 5)
		{
			Vector3 size = voxelEditor.GetMapOrSelectionSize();
			Vector3 center = voxelEditor.GetMapOrSelectionCenter();

			Vector3 direction = side.ToVectorInt();
			Vector3 halfSize = size / 2f;
			Vector3 halfNormalInSize = direction.MultiplyAllAxis(halfSize);
			Vector3 planeOrigin = center + halfNormalInSize;

			// Transform points
			direction = voxelEditor.Transform.TransformDirection(direction);
			planeOrigin = voxelEditor.Transform.TransformPoint(planeOrigin);

			Camera cam = Camera.current;

			Vector3 cameraDir =
					cam.orthographic ?
					cam.transform.forward :
					planeOrigin - cam.transform.position;

			float angle = Vector3.Angle(cameraDir, direction);
			 
			return angle > 90 - angleAllowed;
		}

		protected static void Translate(IVoxelEditor voxelEditor, GeneralDirection3D direction, int steps)
		{
			Vector3 localShift = direction.ToVector() * steps;
			Vector3 globalShift = voxelEditor.Transform.TransformVector(localShift);
			voxelEditor.Transform.position = originalTransformPosition + globalShift;
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
			return center + (dir.MultiplyAllAxis(size) / 2f) + dir * standardSpacing;
		}

		protected static WireShape GetDrawableVoxelSide(VoxelHit hit)
		{
			List<Vector3[]> polygons = new()
			{
				GetVoxelSide(hit.voxelIndex, hit.side, 1f),
				GetVoxelSide(hit.voxelIndex, hit.side, 0.75f),
				GetVoxelSide(hit.voxelIndex, hit.side, 0.5f),
				GetVoxelSide(hit.voxelIndex, hit.side, 0.25f)
			};
			return new WireShape(polygons);
		}
		protected static Vector3[] GetVoxelSide(Vector3Int localCoordinate, GeneralDirection3D side, float sizeMultiplier)
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

		// ------------------ Supported Actions -----------------------------

		protected static readonly VoxelAction[] noVoxelAction = new VoxelAction[0];
		protected static readonly VoxelAction[] allVoxelActions = VoxelEditor_EnumHelper.allVoxelActions;
		public virtual VoxelAction[] GetSupportedActions(IVoxelEditor voxelEditor) => noVoxelAction;

		protected static VoxelAction[] GetTransformActions(IVoxelEditor voxelEditor) =>
			voxelEditor.HasSelection() ? VoxelEditor_EnumHelper.transformActions : noVoxelAction;

		// ------------------ Virtual Methods: Cursor Voxel -----------------------------

		protected virtual bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = false;
			return false;
		}
		protected virtual MapChange OnVoxelCursorDown(IVoxelEditor voxelEditor, VoxelHit hit) => MapChange.None;
		protected virtual MapChange OnVoxelCursorDrag(IVoxelEditor voxelEditor, VoxelHit hit) => MapChange.None;
		protected virtual MapChange OnVoxelCursorUp(IVoxelEditor voxelEditor, VoxelHit hit) => MapChange.None;

		// ------------------ Virtual Methods: Handles -----------------------------

		protected virtual IEnumerable<VoxelHandelInfo> GetHandeles(IVoxelEditor voxelEditor)
		{
			yield break;
		}

		protected virtual MapChange OnHandleClick(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo) => MapChange.None;
		protected virtual MapChange OnHandleDown(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo) => MapChange.None;
		protected virtual MapChange OnHandleDrag(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps) => MapChange.None;
		protected virtual MapChange OnHandleUp(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo, int steps) => MapChange.None;
	}
}
