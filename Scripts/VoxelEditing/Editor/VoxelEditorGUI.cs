#if UNITY_EDITOR

using System.Collections.Generic;
using VoxelSystem.MeshUtility;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EasyEditor;
using MUtility;
using System;
using Unity.Scripting.LifecycleManagement;

namespace VoxelSystem
{
	[NoAutoStaticsCleanup]
	static class VoxelEditorGUI
	{

		static VoxelEditorSettings _iconSettings;
		static Dictionary<VoxelAction, GUIContent> _actionToContent = new();
		static Dictionary<VoxelTool, GUIContent> _toolToContent = new();
		static Dictionary<(VoxelTool, VoxelAction), GUIContent> _toolWithActionToContent = new();

		const int actionButtonHeight = 20;  // 16+4
		const int toolButtonHeight = 36;    // 32 + 4
		const int paletteButtonSize = 26;
		static readonly float _singleLineHeight = EditorGUIUtility.singleLineHeight;
		static readonly float _vSpacing = EditorGUIUtility.standardVerticalSpacing;

		static readonly VoxelTool[] _transformTools =
			VoxelEditor_EnumHelper.allVoxelTools.Where(t => t.IsTransformTool()).ToArray();

		static readonly VoxelTool[] _cursorTools =
			VoxelEditor_EnumHelper.allVoxelTools.Where(t => t.IsCursorTool()).ToArray();

		static GUIStyle _headerStyle;
		static GUIStyle _paletteDarkStyle;
		static Texture _warningIcon;

		static GUIStyle _notSelectedButtonStyle;
		static GUIStyle _selectedButtonStyle;
		static GUIStyle _selectedButtonAttachStyle;
		static GUIStyle _selectedButtonEraseStyle;
		static GUIStyle _selectedButtonOverWriteStyle;
		static GUIStyle _selectedButtonRecolorStyle;
		static GUIStyle _paletteButton;

		static GUIStyle GetSelectedButtonStyle(VoxelAction current, VoxelAction[] supported)
		{
			if (!supported.Contains(current))
			{
				if (supported.IsEmpty())
					return _selectedButtonStyle;
				current = supported.FirstOrDefault();
			}
			return
				current == VoxelAction.Attach ? _selectedButtonAttachStyle :
				current == VoxelAction.Erase ? _selectedButtonEraseStyle :
				current == VoxelAction.Overwrite ? _selectedButtonOverWriteStyle :
				current == VoxelAction.Repaint ? _selectedButtonRecolorStyle : _selectedButtonStyle;
		}

		public static void SetupGuiContentAndStyle()
		{
			_iconSettings = VoxelEditorSettings.Instance;
			if (_headerStyle == null)
			{
				try
				{
					_headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
					_paletteDarkStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
					_notSelectedButtonStyle = new GUIStyle(GUI.skin.button);
					_selectedButtonStyle = new GUIStyle(GUI.skin.button);
					_selectedButtonAttachStyle = new GUIStyle(GUI.skin.button);
					_selectedButtonEraseStyle = new GUIStyle(GUI.skin.button);
					_selectedButtonOverWriteStyle = new GUIStyle(GUI.skin.button);
					_selectedButtonRecolorStyle = new GUIStyle(GUI.skin.button);
					_paletteButton = new GUIStyle(GUI.skin.button);
					_selectedButtonStyle.normal.background = _iconSettings.selectedButton;
					_selectedButtonAttachStyle.normal.background = _iconSettings.selectedButtonAttach;
					_selectedButtonEraseStyle.normal.background = _iconSettings.selectedButtonErase;
					_selectedButtonOverWriteStyle.normal.background = _iconSettings.selectedButtonOverWrite;
					_selectedButtonRecolorStyle.normal.background = _iconSettings.selectedButtonRecolor;

				}
				catch (NullReferenceException) { }
			}

			_warningIcon = EditorGUIUtility.IconContent("Warning").image;

			if (!_actionToContent.IsNullOrEmpty() && !_toolWithActionToContent.IsNullOrEmpty())
				return;

			_iconSettings = VoxelEditorSettings.Instance;

			_actionToContent = new Dictionary<VoxelAction, GUIContent>();
			_toolToContent = new Dictionary<VoxelTool, GUIContent>();
			_toolWithActionToContent = new Dictionary<(VoxelTool, VoxelAction), GUIContent>();

			foreach (VoxelTool tool in VoxelEditor_EnumHelper.allVoxelTools)
			{
				Texture texture = _iconSettings.GetToolIcon(tool);
				if (texture != null)
				{
					string toolTip = tool.ToString();
					GUIContent content = new(texture, toolTip);
					_toolToContent.Add(tool, content);
				}
			}


			foreach (VoxelAction action in VoxelEditor_EnumHelper.allVoxelActions)
			{
				Texture texture = _iconSettings.GetActionIcon(action);
				string label = action.GetLabel();
				string tooltip = action.GetTooltip();
				GUIContent content = new(label, texture, tooltip);
				_actionToContent.Add(action, content);

				foreach (VoxelTool tool in VoxelEditor_EnumHelper.allVoxelTools)
				{
					texture = _iconSettings.GetToolIcon(tool, action);
					if (texture != null)
					{
						string toolTip = tool.ToString() + " - " + action.ToString();
						content = new(texture, toolTip);
						_toolWithActionToContent.Add((tool, action), content);
					}
				}
			}
		}

		// ---------------------------- Drawing Methods ----------------------------

		public static void DrawHeader(VoxelEditor voxelEditor, ref Rect position)
		{
			const float negativeSpace = -6;
			if (voxelEditor.enabled)
			{
				EditorGUI.LabelField(position.SliceOutLine(), "Map Name:   " + voxelEditor.MapName, _headerStyle);
				position.RemoveSpace(negativeSpace);
				EditorGUI.LabelField(position.SliceOutLine(), voxelEditor.Map.FullSize.ToString(), EditorStyles.centeredGreyMiniLabel);
				position.RemoveOneSpace();
			}
			else
			{
				GUI.color = EditorHelper.ErrorRedColor;
				EditorGUI.LabelField(position.SliceOutLine(), "Editing is disabled!", _headerStyle);
				position.RemoveSpace(negativeSpace);

				EditorGUI.LabelField(position.SliceOutLine(), "Enable the VoxelEditor component to edit voxel map.", EditorStyles.centeredGreyMiniLabel);
				GUI.color = Color.white;
			}
		}

		public static void DrawControlPanel(VoxelEditor voxelEditor, ref Rect position)
		{
			bool tempEnabled = GUI.enabled;
			GUI.enabled = voxelEditor.IsEditingEnabled();

			VoxelAction selectedAction = voxelEditor.SelectedAction;
			VoxelTool selectedTool = voxelEditor.SelectedTool;
			DrawVoxelActions(voxelEditor, selectedAction, selectedTool, ref position);
			DrawToolRow(voxelEditor, _cursorTools, selectedTool, selectedAction, ref position);
			DrawToolRow(voxelEditor, _transformTools, selectedTool, selectedAction, ref position);
			position.RemoveOneSpace();
			DrawSelectionTools(voxelEditor, ref position);
			position.RemoveOneSpace();
			GUI.enabled = tempEnabled;
		}

		public static void DrawMapActions(VoxelEditor voxelEditor, ref Rect position)
		{
			bool tempEnabled = GUI.enabled;
			bool enableEdit = voxelEditor.IsEditingEnabled();
			GUI.enabled = enableEdit;

			DrawMapCommands(voxelEditor, ref position);
			DrawTransformLocks(voxelEditor, ref position);
			position.RemoveOneSpace();

			GUI.enabled = tempEnabled;
		}

		static void DrawMapCommands(VoxelEditor voxelEditor, ref Rect position)
		{
			bool tempEnabled = GUI.enabled;
			bool enableEdit = voxelEditor.IsEditingEnabled();
			GUI.enabled = enableEdit;

			Rect rect = position.SliceOut(_singleLineHeight);
			int count = 3;
			float width = (rect.width - (count - 1) * _vSpacing) / count;
			rect.width = width;

			bool change = false;
			rect.height = _singleLineHeight;
			if (GUI.Button(rect, "Clear Map"))
			{
				voxelEditor.RecordForUndo("Map Cleared", RecordType.Map);
				change = voxelEditor.Map.ClearWhole();
			}

			rect.x += width + _vSpacing;
			if (GUI.Button(rect, "Fill Map"))
			{
				voxelEditor.RecordForUndo("Map Filled", RecordType.Map);
				change = voxelEditor.Map.SetWhole(voxelEditor.SelectedVoxelValue);
			}
			rect.x += width + _vSpacing;
			GUI.enabled = VoxelClipboard.HaveContent && enableEdit;
			if (GUI.Button(rect, "SetupFromMesh from Clipboard"))
			{
				voxelEditor.RecordForUndo("Selection Cleared", RecordType.Map | RecordType.Transform | RecordType.Editor);
				voxelEditor.Map.SetupFrom(VoxelClipboard.ClipboardMap);
				if (voxelEditor.HasSelection())
					voxelEditor.transform.position += voxelEditor.Selection.min;
				voxelEditor.Deselect();
				change |= true;
			}

			if (change)
				voxelEditor.Map.MapChanged(MapChange.Final);

			GUI.enabled = tempEnabled;
		}

		static void DrawTransformLocks(VoxelEditor voxelEditor, ref Rect position)
		{
			bool tempEnabled = GUI.enabled;
			bool enableEdit = voxelEditor.IsEditingEnabled();
			GUI.enabled = enableEdit;
			Rect fullLineRect = position.SliceOutLine();
			TransformLock tLock = voxelEditor.transformLock;

			int count = 3;
			float width = (fullLineRect.width - (count - 1) * _vSpacing) / count;
			Rect rect = new(fullLineRect.x, fullLineRect.y, width, fullLineRect.height);
			TransformLock tLockOriginal = tLock;

			tLock.position = DrawOneLock(tLock.position, "Position");
			tLock.rotation = DrawOneLock(tLock.rotation, "Rotation");

			//rect.width = width * 4 + 3 * spacing;
			tLock.scale = DrawOneLock(tLock.scale, "Scale");

			if (!Equals(tLockOriginal, tLock))
			{
				voxelEditor.TransformLock = tLock;
				// TODO: SET DIRTY
			}

			GUI.color = Color.white;
			GUI.enabled = enableEdit && tLock.rotation && tLock.scale;
			fullLineRect = position.SliceOutLine();
			GUIContent content = new("Apply Transform to Map", "Apply Transform rotation & scale to the Map (Need to lock rotation and scale)");
			if (GUI.Button(fullLineRect, content))
			{
				VoxelMap map = voxelEditor.Map;
				Transform transform = voxelEditor.transform;
				voxelEditor.RecordForUndo("Rotation & Scale Applied to Map", RecordType.Map | RecordType.Transform);
				map.ApplyScale(transform);
				map.ApplyRotation(transform);
			}
			GUI.enabled = enableEdit;

			GUI.enabled = tempEnabled;

			bool DrawOneLock(bool b, string text)
			{

				GUIContent cont = new(text, b ? _iconSettings.lockOnIcon : _iconSettings.lockOffIcon, "Locking Transform " + text + " to whole values");
				GUI.color = b ? new Color(0.75f, 0.75f, 0.75f) : Color.white;
				if (GUI.Button(rect, cont))
					b = !b;
				rect.x += width + _vSpacing;
				return b;
			}
		}

		static void DrawSelectionTools(VoxelEditor voxelEditor, ref Rect position)
		{
			bool tempEnabled = GUI.enabled;
			bool enableEdit = voxelEditor.IsEditingEnabled();
			GUI.enabled = enableEdit;
			Rect fullRow = position.SliceOut(_singleLineHeight * 4 + 3 * _vSpacing);

			int count = 3;
			float width = (fullRow.width - (count - 1) * _vSpacing) / count;
			Rect buttonRect = fullRow;
			buttonRect.width = width;

			buttonRect.height = _singleLineHeight * 2 + _vSpacing;
			DrawVoxelTool(voxelEditor, voxelEditor.SelectedTool, voxelEditor.SelectedAction, buttonRect, VoxelTool.Select);
			buttonRect.x += width + _vSpacing;

			bool change = false;
			buttonRect.height = _singleLineHeight;

			Vector3Int fullMapSize = voxelEditor.Map.FullSize;
			GUI.enabled = enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, " De-Select"))
			{
				voxelEditor.RecordForUndo("Remove Selection", RecordType.Editor);
				voxelEditor.Deselect();
			}
			buttonRect.x += width + _vSpacing;
			GUI.enabled = enableEdit && voxelEditor.Selection.size != fullMapSize;
			if (GUI.Button(buttonRect, " SelectAll"))
			{
				voxelEditor.RecordForUndo("Remove Selection", RecordType.Editor);
				voxelEditor.Selection = new BoundsInt(Vector3Int.zero, fullMapSize);
			}
			GUI.enabled = enableEdit;

			buttonRect.x -= width + _vSpacing;
			buttonRect.y += _singleLineHeight + _vSpacing;
			GUI.enabled = enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, " Copy"))
			{
				voxelEditor.Copy();
			}
			buttonRect.x += width + _vSpacing;
			GUI.enabled = enableEdit && VoxelClipboard.HaveContent;
			if (GUI.Button(buttonRect, " Paste"))
			{
				voxelEditor.RecordForUndo("Voxel Selection Pasted", RecordType.Map | RecordType.Editor);
				voxelEditor.Paste();
				change = true;
			}

			width = (fullRow.width - _vSpacing) / 2;
			buttonRect = fullRow;
			buttonRect.y += _singleLineHeight * 2 + 2 * _vSpacing;
			buttonRect.height = _singleLineHeight;
			buttonRect.width = width;

			GUI.enabled = enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, "Clear Selection"))
			{
				voxelEditor.RecordForUndo("Selection Cleared", RecordType.Map);
				change = voxelEditor.ClearInsideSelection();
			}

			buttonRect.x += width + _vSpacing;
			GUI.enabled = enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, "Fill Selection"))
			{
				voxelEditor.RecordForUndo("Selection Filled", RecordType.Map);
				change = voxelEditor.FillInsideSelection();
			}

			buttonRect.y += _singleLineHeight + _vSpacing;
			buttonRect.x -= width + _vSpacing;

			Transform upperSibling = voxelEditor.transform.GetUpperSibling();
			VoxelEditor upperVoxelEditor = upperSibling == null ? null : upperSibling.GetComponent<VoxelEditor>();
			string name = upperSibling == null ? "-" : upperSibling.name;
			GUI.enabled = upperVoxelEditor != null;
			string toolTip = upperVoxelEditor != null
				? $"Works best if the rotation and scale is the same.\nNeed to be inside the destination map's bounds."
				: "Destination need to have a VoxelEditor component";
			GUIContent content = new($"Merge Up: {name}", toolTip);
			if (GUI.Button(buttonRect, content))
			{
				voxelEditor.MergeInto(upperVoxelEditor);
			}
			buttonRect.x += width + _vSpacing;
			GUI.enabled = enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, "Separate Selection"))
			{
				voxelEditor.SeparateSelectionToGameObject();
				change = true;
			}
			GUI.enabled = enableEdit;

			if (change)
				voxelEditor.Map.MapChanged(MapChange.Final);

			GUI.enabled = tempEnabled;
		}

		static void DrawToolRow(VoxelEditor voxelEditor, VoxelTool[] tools, VoxelTool selectedTool, VoxelAction selectedAction, ref Rect position)
		{
			Rect fullRowRect = position.SliceOut(toolButtonHeight);

			int count = tools.Length;
			float width = (fullRowRect.width - (count - 1) * _vSpacing) / count;
			Rect rect = new(fullRowRect.x, fullRowRect.y, width, fullRowRect.height);

			foreach (VoxelTool tool in tools)
			{
				DrawVoxelTool(voxelEditor, selectedTool, selectedAction, rect, tool);
				rect.x += width + _vSpacing;
			}
			GUI.color = Color.white;
		}

		static void DrawVoxelTool(IVoxelEditor voxelEditor, VoxelTool selectedTool, VoxelAction selectedAction, Rect rect, VoxelTool drawnTool)
		{
			bool isActionSupported = false;
			if (drawnTool != VoxelTool.None)
			{
				VoxelAction[] supportedActions = drawnTool.GetHandler().GetSupportedActions(voxelEditor);
				isActionSupported = supportedActions.Contains(selectedAction);

				if (drawnTool == selectedTool && !isActionSupported && !supportedActions.IsEmpty())
					voxelEditor.SelectedAction = supportedActions[0];
			}

			GUIContent content = isActionSupported
				? _toolWithActionToContent[(drawnTool, selectedAction)]
				: _toolToContent[drawnTool];

			GUIStyle style = selectedTool == drawnTool
				? GetSelectedButtonStyle(selectedAction, selectedTool.GetHandler().GetSupportedActions(voxelEditor))
				: _notSelectedButtonStyle;

			if (GUI.Button(rect, content, style))
			{
				voxelEditor.SelectedTool = voxelEditor.SelectedTool == drawnTool
					? VoxelTool.None : drawnTool;
			}
		}

		static void DrawVoxelActions(VoxelEditor voxelEditor, VoxelAction selectedAction, VoxelTool selectedTool, ref Rect position)
		{
			bool tempEnabled = GUI.enabled;
			bool enableEdit = voxelEditor.IsEditingEnabled();
			GUI.enabled = enableEdit;
			Rect fullLine = position.SliceOut(actionButtonHeight);

			int count = VoxelEditor_EnumHelper.allVoxelActions.Length;
			float width = (fullLine.width - (count - 1) * _vSpacing) / count;
			Rect rect = new(fullLine.x, fullLine.y, width, fullLine.height);

			VoxelAction[] supportedActions = selectedTool == VoxelTool.None
				? VoxelEditor_EnumHelper.allVoxelActions
				: selectedTool.GetHandler().GetSupportedActions(voxelEditor);

			foreach (VoxelAction action in VoxelEditor_EnumHelper.allVoxelActions)
			{
				GUIContent content = _actionToContent[action];
				bool isActionEnabled = supportedActions.Contains(action);
				bool isSelected = selectedAction == action;

				GUI.enabled = enableEdit && isActionEnabled;

				GUIStyle style = !isSelected ? _notSelectedButtonStyle :
					action == VoxelAction.Attach ? _selectedButtonAttachStyle :
					action == VoxelAction.Erase ? _selectedButtonEraseStyle :
					action == VoxelAction.Overwrite ? _selectedButtonOverWriteStyle :
					action == VoxelAction.Repaint ? _selectedButtonRecolorStyle : _selectedButtonStyle;

				if (GUI.Button(rect, content, style))
					voxelEditor.SelectedAction = action;

				rect.x += width + _vSpacing;
			}

			GUI.enabled = tempEnabled;
			GUI.backgroundColor = Color.white;
		}

		public static void DrawPalettes(VoxelEditor voxelEditor, ref Rect position)
		{
			bool enableEdit = voxelEditor.IsEditingEnabled();
			bool tempEnabled = GUI.enabled;
			GUI.enabled = enableEdit;
			DrawPalette(
				voxelEditor,
				voxelEditor.MaterialPalette.Select(item => item.name).ToList(),
				voxelEditor.SelectedMaterialIndex,
				index => index,
				index => index < voxelEditor.MaterialPalette.Count && index >= 0,
				newSelectedIndex => voxelEditor.SelectedMaterialIndex = (byte)newSelectedIndex,
				VoxelTool.MaterialPicker,
				new("Material Index:", "The selected index of the Material names"),
				ref position);

			DrawPalette(
				voxelEditor,
				voxelEditor.ShapePalette.GetNames(),
				voxelEditor.SelectedShapeId,
				index => voxelEditor.ShapePalette.GetBuilderByIndex(index).VoxelId,
				value => voxelEditor.ShapePalette.ContainsID(value),
				newSelectedID => voxelEditor.SelectedShapeId = newSelectedID,
				VoxelTool.ShapePicker,
				new("Shape Id:", "The selected index of the Shape names"),
				ref position);
			GUI.enabled = tempEnabled;
		}

		const int itemsInARow = 3;
		public static float GetPaletteHeight(int count)
		{
			if (count == 0) return 0;
			int rows = Mathf.CeilToInt(count / (float)itemsInARow);
			return rows * paletteButtonSize + (rows - 1) * _vSpacing;
		}

		static void DrawPalette(
			IVoxelEditor voxelEditor, IEnumerable<string> names, int selectedValue,
			Func<int, int> indexToValue,
			Func<int, bool> isValidValue,
			Action<int> onSelect,
			VoxelTool voxel, GUIContent title, ref Rect position)
		{
			GUI.color = Color.white;
			Rect oneRowRect = position.SliceOutLine();

			Rect rect = oneRowRect;
			const float colorPickerWidth = 30;
			rect.width = colorPickerWidth;
			DrawVoxelTool(voxelEditor, voxelEditor.SelectedTool, voxelEditor.SelectedAction, rect, voxel);

			rect.x += colorPickerWidth + _vSpacing;
			rect.width = oneRowRect.width - colorPickerWidth + _vSpacing;

			if (!isValidValue(selectedValue))
			{
				title.tooltip = "This index is over the list's range!";
				title.image = _warningIcon;
			}


			int newValue = EditorGUI.IntField(rect, title, selectedValue);
			newValue = Math.Max(newValue, 0);

			int index = 0;
			float itemWidth = (oneRowRect.width - (itemsInARow - 1) * _vSpacing) / itemsInARow;
			if (names != null)
				foreach (string name in names)
				{
					if (name == null) continue;

					if (index % itemsInARow == 0)
					{
						oneRowRect = position.SliceOut(paletteButtonSize);
						oneRowRect.width = itemWidth;
					}

					GUI.color = indexToValue(index) == selectedValue ? Color.white : new Color(1, 1, 1, 0.4f);
					bool click = GUI.Button(oneRowRect, name, _paletteButton);

					GUI.Label(oneRowRect, name, _paletteDarkStyle);
					if (click)
						newValue = indexToValue(index);

					oneRowRect.x += (itemWidth + _vSpacing);
					index++;
				}


			if (newValue != selectedValue)
			{
				Undo.RecordObject(voxelEditor.EditorObject, "Selected Value Changed");
				if (voxelEditor.SelectedAction == VoxelAction.Erase)
					voxelEditor.SelectedAction = VoxelAction.Attach;

				onSelect(newValue);
			}
			GUI.color = Color.white;
		}

		public static void DrawCubicTransformation(VoxelEditor voxelEditor, ref Rect position)
		{
			if (voxelEditor.SelectedShape == null) return;
			if (!voxelEditor.SelectedShape.SupportsTransformation) return;

			Voxel voxelValue = voxelEditor.SelectedVoxelValue;
			CubicTransformation transformation = voxelValue.CubicTransformation;
			voxelValue.CubicTransformation = VoxelShapeBuilderEditor.DrawCubicTransformation(transformation, ref position);
			voxelEditor.SelectedVoxelValue = voxelValue;
		}

		public static void DrawExtraControls(VoxelEditor voxelEditor, ref Rect position)
		{
			Voxel voxelValue = voxelEditor.SelectedVoxelValue;
			voxelValue.extraData = VoxelShapeBuilderEditor.DrawExtraControls(
				voxelEditor.SelectedShape, voxelEditor.SelectedVoxelValue.extraData, ref position);
			voxelEditor.SelectedVoxelValue = voxelValue;
		}

		// Draw Preview

		static readonly CustomMeshPreview _customMeshPreview = new();
		static readonly MeshBuilder _previewMeshBuilder = new();
		static Mesh _previewMesh;
		static Mesh GetPreviewMesh() => _previewMesh;
		static int _lastPreviewedShapeIndex = 0;
		static byte _lastCubicTransformation = 0;
		static byte _lastExtraVoxelData = 0;

		public static void DrawVoxelPreview(VoxelEditor voxelEditor, ref Rect position, Side drawTo)
		{
			if (voxelEditor.ShapePalette == null) return;
			VoxelShapeBuilder shape = voxelEditor.ShapePalette.GetBuilder(voxelEditor.SelectedShapeId);
			if (shape == null) return;
			if (Event.current.type != EventType.Repaint) return;

			Rect rect = position.SliceOut(150, drawTo);

			_customMeshPreview.TextureSize = new Vector2(rect.width, rect.height);
			_customMeshPreview.BackgroundType = CameraClearFlags.Skybox;
			Vector3 cameraEulerAngles = SceneView.lastActiveSceneView.camera.transform.eulerAngles;
			_customMeshPreview.CameraAngle = new Vector2(-cameraEulerAngles.y, cameraEulerAngles.x);
			_customMeshPreview.Material = voxelEditor.SelectedMaterial;
			_customMeshPreview.meshGetter = GetPreviewMesh;
			_customMeshPreview.SetDirty();

			int shapeId = voxelEditor.SelectedShapeId;
			byte cubicTransformation = voxelEditor.SelectedVoxelValue.cubicTransformationIndex;
			byte extraVoxelData = voxelEditor.SelectedVoxelValue.extraData;

			if (shapeId != _lastPreviewedShapeIndex ||
				cubicTransformation != _lastCubicTransformation ||
				_lastExtraVoxelData != extraVoxelData)
			{
				_lastPreviewedShapeIndex = shapeId;
				_lastCubicTransformation = cubicTransformation;
				_lastExtraVoxelData = extraVoxelData;

				ArrayVoxelMap map = ArrayVoxelMap.GetTestOneVoxelMap(voxelEditor.SelectedVoxelValue);
				_previewMeshBuilder.Clear();
				shape.GenerateMeshData(map, new() { Vector3Int.one }, shapeId, _previewMeshBuilder, false);

				if (_previewMesh == null)
					_previewMesh = new Mesh();
				else
					_previewMesh.Clear();

				_previewMeshBuilder.CopyToMesh(_previewMesh);
			}

			EditorGUI.DrawPreviewTexture(rect, _customMeshPreview.PreviewTexture);
		}
	}
}
#endif