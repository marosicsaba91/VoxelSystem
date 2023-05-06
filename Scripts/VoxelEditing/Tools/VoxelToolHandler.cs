using MUtility;
using System;
using System.Collections.Generic;
using UnityEditor;
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
		// Raycast Helper Variables
		protected static ArrayVoxelMap _originalMap = null; // Target Voxel Map before Mouse Down
		protected static bool _isLastRayHit;
		protected static bool _isMouseDown;
		protected static VoxelHit _lastValidHit;
		protected static VoxelHit _mouseDownHit;
		protected static VoxelHit _lastHandledHit;

		// Handle Helper Variables
		protected static Vector3 _originalTransformPosition = Vector3.zero;
		protected static Vector3 _lastHandleVector;
		protected static bool _isDragged = false;
		protected static GeneralDirection3D _dragDirection;
		protected static int _handleSteps = 0;
		protected static float _standardSpacing = 1;
		protected static GUIStyle textStyle = new GUIStyle()
		{
			alignment = TextAnchor.MiddleCenter,
			fontSize = 15,
			normal = new GUIStyleState() { textColor = Color.white }
		};

		// --------------------- CONTROL ---------------------------

		internal void ExecuteEditorControl(IVoxelEditor voxelEditor, Event guiEvent, Ray ray)
		{
			Matrix4x4 matrix4X4 = Handles.matrix;
			Handles.matrix = voxelEditor.transform.localToWorldMatrix;

			TryExecuteRaycast(voxelEditor, guiEvent, ray);
			ExecutedHandles(voxelEditor, guiEvent);

			Handles.matrix = matrix4X4;
		}

		// --------------------- Raycast ---------------------

		void TryExecuteRaycast(IVoxelEditor voxelEditor, Event guiEvent, Ray ray)
		{
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
					Color color = Color.red;
					Handles.color = new(color.r, color.g, color.b, color.a / 4f);
					Handles.DrawWireCube((Vector3)_lastValidHit.voxelIndex + Vector3.one * 0.5f, Vector3.one);

					Handles.color = color;
					Drawable drawable = GetDrawableVoxelSide(_lastValidHit);
					drawable.DrawHandle();


				}
			}
		}

		void HandleRaycast(IVoxelEditor voxelEditor, Ray ray, Event guiEvent, bool raycastOutside)
		{
			if (!guiEvent.isMouse || guiEvent.button != 0) return;

			Transform transform = voxelEditor.transform;
			VoxelMap map = _isMouseDown ? _originalMap : voxelEditor.Map;
			_isLastRayHit = map.Raycast(ray, out VoxelHit hit, transform, raycastOutside);

			if (_isLastRayHit)
				_lastValidHit = hit;

			if (guiEvent.type == EventType.MouseDown)
				HandleCursorDown(voxelEditor, _isLastRayHit, hit);
			else if (!_isMouseDown)
			{
				guiEvent.Use();
				return;
			}

			if (guiEvent.type == EventType.MouseDrag)
				HandleCursorDrag(voxelEditor, _isLastRayHit, hit);
			else if (guiEvent.type == EventType.MouseUp)
			{
				if (OnVoxelCursorUp(voxelEditor, _lastValidHit))
					voxelEditor.Map.MapChanged();
				_isMouseDown = false;
			}

			guiEvent.Use();
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

		void ExecutedHandles(IVoxelEditor voxelEditor, Event guiEvent)
		{
			foreach (VoxelHandelInfo handleInfo in GetHandeles(voxelEditor))
				ExecuteOneHandle(voxelEditor, handleInfo);

			if (guiEvent.isMouse && guiEvent.button == 0) // Left Mouse
				guiEvent.Use();
		}
		void ExecuteOneHandle(IVoxelEditor voxelEditor, VoxelHandelInfo handleInfo)
		{
			VoxelMap map = voxelEditor.Map;
			VoxelTool _tool = voxelEditor.SelectedTool;
			GeneralDirection3D direction = handleInfo.direction;
			Axis3D axis = direction.GetAxis();
			Vector3 handlePos = handleInfo.position;
			float sizeMultiplier = map.FullSize.AbsMean() / 20f;  // 20 looks good
			_standardSpacing = sizeMultiplier * 2;

			Color color = axis.GetAxisColor();
			Color focused = (color + Color.white) / 2f;
			Handles.color = color;

			Vector3 directionVector = direction.ToVector();
			var rotation = Quaternion.LookRotation(directionVector);
			
			Handles.CapFunction capFunction = handleInfo.coneType == HandeleConeType.Arrow
				? Handles.ConeHandleCap
				: Handles.CubeHandleCap;

			if(handleInfo.coneType == HandeleConeType.Box)
				sizeMultiplier *= 0.7f;

			AdvancedHandles.HandleResult handleResult =
				AdvancedHandles.Handle(handlePos, rotation, sizeMultiplier, capFunction, color, focused, color);


			// Handle Click
			if (handleResult.handleEvent is HandleEvent.LmbClick or HandleEvent.LmbDoubleClick) 
			{
				if (OnHandleClick(voxelEditor, handleInfo))
				{
					voxelEditor.Map.MapChanged();
					return;
				}
			}

			// Handle Drag
			switch (handleResult.handleEvent)
			{
				case HandleEvent.LmbPress: // START DRAG 
					_isDragged = true;
					_dragDirection = direction;
					_lastHandleVector = Vector3Int.zero;
					_originalMap ??= new ArrayVoxelMap();
					_originalMap.SetupFrom(map);
					_originalTransformPosition = voxelEditor.transform.position;
					if (OnHandleDown(voxelEditor, handleInfo))
						voxelEditor.Map.MapChanged();
					break;

				case HandleEvent.LmbDrag: // DRAG 
					_isDragged = true;
					_handleSteps = (int)Vector3.Dot(handleResult.IsDragged, directionVector);
					Vector3Int vec = direction.ToVectorInt() * _handleSteps;
					bool changed = _lastHandleVector != vec;
					_lastHandleVector = vec;
					if (changed)
						if (OnHandleDrag(voxelEditor, handleInfo, _handleSteps))
							voxelEditor.Map.MapChanged();
					break;

				case HandleEvent.LmbRelease: // RELEASE 
					_handleSteps = (int)Vector3.Dot(handleResult.IsDragged, directionVector);
					if (OnHandleUp(voxelEditor, handleInfo, _handleSteps))
						voxelEditor.Map.MapChanged();
					_handleSteps = 0;
					_lastHandleVector = Vector3Int.zero;
					_isDragged = false;

					break;
			}

			if (!handleInfo.text.IsNullOrEmpty())
			{
				textStyle.normal.textColor = (color + Color.white) / 2f;
				Handles.Label(handlePos + Vector3.up * _standardSpacing, handleInfo.text, textStyle);
			}
		}

		protected void Reset(IVoxelEditor voxelEditor) 
		{
			voxelEditor.Map.SetupFrom(_originalMap);
			voxelEditor.transform.position = _originalTransformPosition;
		}

		// ------------------ Static Methods -----------------------------

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

		protected static Vector3 GetMapSidePosition(Vector3 mapSize, GeneralDirection3D direction)
		{
			Vector3 dir = direction.ToVector();
			Vector3 center = mapSize / 2f;
			return center + (dir.MultiplyAllAxis(mapSize) / 2f) + dir * _standardSpacing;
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
		public virtual VoxelAction[] SupportedActions => noVoxelAction;

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
