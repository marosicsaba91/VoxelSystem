#if UNITY_EDITOR

using MUtility;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using EasyInspector;

namespace VoxelSystem
{
	static class VoxelEditorGUI
	{

		static VoxelEditorSettings iconSettings;
		static Dictionary<VoxelAction, GUIContent> _actionToContent = new();
		static Dictionary<VoxelTool, GUIContent> _toolToContent = new();
		static Dictionary<(VoxelTool, VoxelAction), GUIContent> _toolWithActionToContent = new();

		const int _actionButtonHeight = 20;  // 16+4
		const int _toolButtonHeight = 36;    // 32 + 4
		const int _paletteButtonSize = 26;
		static readonly float singleLineHeight = EditorGUIUtility.singleLineHeight;
		static readonly float vSpacing = EditorGUIUtility.standardVerticalSpacing;

		static readonly VoxelTool[] transformTools =
			VoxelEditor_EnumHelper.allVoxelTools.Where(t => t.IsTransformTool()).ToArray();

		static readonly VoxelTool[] cursorTools =
			VoxelEditor_EnumHelper.allVoxelTools.Where(t => t.IsCursorTool()).ToArray();

		static Texture warningIcon;
		static GUIStyle headerStyle;
		static GUIStyle paletteDarkStyle;

		static GUIStyle notSelectedButtonStyle;
		static GUIStyle selectedButtonStyle;
		static GUIStyle selectedButtonAttachStyle;
		static GUIStyle selectedButtonEraseStyle;
		static GUIStyle selectedButtonOverWriteStyle;
		static GUIStyle selectedButtonRecolorStyle;
		static GUIStyle paletteButton;

		static GUIStyle GetSelectedButtonStyle(VoxelAction current, VoxelAction[] supported)
		{
			if (!supported.Contains(current))
			{
				if (supported.IsEmpty())
					return selectedButtonStyle;
				current = supported.FirstOrDefault();
			}
			return
				current == VoxelAction.Attach ? selectedButtonAttachStyle :
				current == VoxelAction.Erase ? selectedButtonEraseStyle :
				current == VoxelAction.Overwrite ? selectedButtonOverWriteStyle :
				current == VoxelAction.Repaint ? selectedButtonRecolorStyle : selectedButtonStyle;
		}

		public static void SetupGuiContentAndStyle()
		{
			iconSettings = VoxelEditorSettings.Instance;
			if (headerStyle == null)
			{
				try
				{
					headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
					paletteDarkStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
					notSelectedButtonStyle = new GUIStyle(GUI.skin.button);
					selectedButtonStyle = new GUIStyle(GUI.skin.button);
					selectedButtonAttachStyle = new GUIStyle(GUI.skin.button);
					selectedButtonEraseStyle = new GUIStyle(GUI.skin.button);
					selectedButtonOverWriteStyle = new GUIStyle(GUI.skin.button);
					selectedButtonRecolorStyle = new GUIStyle(GUI.skin.button);
					paletteButton = new GUIStyle(GUI.skin.button);
					selectedButtonStyle.normal.background = iconSettings.selectedButton;
					selectedButtonAttachStyle.normal.background = iconSettings.selectedButtonAttach;
					selectedButtonEraseStyle.normal.background = iconSettings.selectedButtonErase;
					selectedButtonOverWriteStyle.normal.background = iconSettings.selectedButtonOverWrite;
					selectedButtonRecolorStyle.normal.background = iconSettings.selectedButtonRecolor;

				}
				catch (NullReferenceException) { }
			}

			warningIcon = EditorGUIUtility.IconContent("Warning").image;

			if (!_actionToContent.IsNullOrEmpty() && !_toolWithActionToContent.IsNullOrEmpty())
				return;

			iconSettings = VoxelEditorSettings.Instance;

			_actionToContent = new Dictionary<VoxelAction, GUIContent>();
			_toolToContent = new Dictionary<VoxelTool, GUIContent>();
			_toolWithActionToContent = new Dictionary<(VoxelTool, VoxelAction), GUIContent>();

			foreach (VoxelTool tool in VoxelEditor_EnumHelper.allVoxelTools)
			{
				Texture texture = iconSettings.GetToolIcon(tool);
				if (texture != null)
				{
					string toolTip = tool.ToString();
					GUIContent content = new(texture, toolTip);
					_toolToContent.Add(tool, content);
				}
			}

			foreach (VoxelAction action in VoxelEditor_EnumHelper.allVoxelActions)
			{
				Texture texture = iconSettings.GetActionIcon(action);
				string label = action.GetLabel();
				string tooltip = action.GetTooltip();
				GUIContent content = new(label, texture, tooltip);
				_actionToContent.Add(action, content);

				foreach (VoxelTool tool in VoxelEditor_EnumHelper.allVoxelTools)
				{
					texture = iconSettings.GetToolIcon(tool, action);
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
				EditorGUI.LabelField(position.SliceOutLine(), "Map Name:   " + voxelEditor.MapName, headerStyle);
				position.RemoveSpace(negativeSpace);
				EditorGUI.LabelField(position.SliceOutLine(), voxelEditor.Map.FullSize.ToString(), EditorStyles.centeredGreyMiniLabel);
				position.RemoveOneSpace();
			}
			else
			{
				GUI.color = EditorHelper.ErrorRedColor;
				EditorGUI.LabelField(position.SliceOutLine(), "Editing is disabled!", headerStyle);
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
			DrawToolRow(voxelEditor, cursorTools, selectedTool, selectedAction, ref position);
			DrawToolRow(voxelEditor, transformTools, selectedTool, selectedAction, ref position);
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

			Rect rect = position.SliceOut(singleLineHeight);
			int count = 3;
			float width = (rect.width - (count - 1) * vSpacing) / count;
			rect.width = width;

			bool change = false;
			rect.height = singleLineHeight;
			if (GUI.Button(rect, "Clear Map"))
			{
				voxelEditor.RecordForUndo("Map Cleared", RecordType.Map);
				change = voxelEditor.Map.ClearWhole();
			}

			rect.x += width + vSpacing;
			if (GUI.Button(rect, "Fill Map"))
			{
				voxelEditor.RecordForUndo("Map Filled", RecordType.Map);
				change = voxelEditor.Map.SetWhole(voxelEditor.SelectedVoxelValue);
			}
			rect.x += width + vSpacing;
			GUI.enabled = VoxelClipboard.HaveContent && enableEdit;
			if (GUI.Button(rect, "Setup from Clipboard"))
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
			float width = (fullLineRect.width - (count - 1) * vSpacing) / count;
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

				GUIContent cont = new(text, b ? iconSettings.lockOnIcon : iconSettings.lockOffIcon, "Locking Transform " + text + " to whole values");
				GUI.color = b ? new Color(0.75f, 0.75f, 0.75f) : Color.white;
				if (GUI.Button(rect, cont))
					b = !b;
				rect.x += width + vSpacing;
				return b;
			}
		}

		static void DrawSelectionTools(VoxelEditor voxelEditor, ref Rect position)
		{
			bool tempEnabled = GUI.enabled;
			bool enableEdit = voxelEditor.IsEditingEnabled();
			GUI.enabled = enableEdit;
			Rect fullRow = position.SliceOut(singleLineHeight * 4 + 3 * vSpacing);

			int count = 3;
			float width = (fullRow.width - (count - 1) * vSpacing) / count;
			Rect buttonRect = fullRow;
			buttonRect.width = width;

			buttonRect.height = singleLineHeight * 2 + vSpacing;
			DrawVoxelTool(voxelEditor, voxelEditor.SelectedTool, voxelEditor.SelectedAction, buttonRect, VoxelTool.Select);
			buttonRect.x += width + vSpacing;

			bool change = false;
			buttonRect.height = singleLineHeight;

			Vector3Int fullMapSize = voxelEditor.Map.FullSize;
			GUI.enabled = enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, " De-Select"))
			{
				voxelEditor.RecordForUndo("Remove Selection", RecordType.Editor);
				voxelEditor.Deselect();
			}
			buttonRect.x += width + vSpacing;
			GUI.enabled = enableEdit && voxelEditor.Selection.size != fullMapSize;
			if (GUI.Button(buttonRect, " SelectAll"))
			{
				voxelEditor.RecordForUndo("Remove Selection", RecordType.Editor);
				voxelEditor.Selection = new BoundsInt(Vector3Int.zero, fullMapSize);
			}
			GUI.enabled = enableEdit;

			buttonRect.x -= width + vSpacing;
			buttonRect.y += singleLineHeight + vSpacing;
			GUI.enabled = enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, " Copy"))
			{
				voxelEditor.Copy();
			}
			buttonRect.x += width + vSpacing;
			GUI.enabled = enableEdit && VoxelClipboard.HaveContent;
			if (GUI.Button(buttonRect, " Paste"))
			{
				voxelEditor.RecordForUndo("Voxel Selection Pasted", RecordType.Map | RecordType.Editor);
				voxelEditor.Paste();
				change = true;
			}

			width = (fullRow.width - vSpacing) / 2;
			buttonRect = fullRow;
			buttonRect.y += singleLineHeight * 2 + 2 * vSpacing;
			buttonRect.height = singleLineHeight;
			buttonRect.width = width;

			GUI.enabled = enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, "Clear Selection"))
			{
				voxelEditor.RecordForUndo("Selection Cleared", RecordType.Map);
				change = voxelEditor.ClearInsideSelection();
			}

			buttonRect.x += width + vSpacing;
			GUI.enabled = enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, "Fill Selection"))
			{
				voxelEditor.RecordForUndo("Selection Filled", RecordType.Map);
				change = voxelEditor.FillInsideSelection();
			}

			buttonRect.y += singleLineHeight + vSpacing;
			buttonRect.x -= width + vSpacing;

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
			buttonRect.x += width + vSpacing;
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
			Rect fullRowRect = position.SliceOut(_toolButtonHeight);

			int count = tools.Length;
			float width = (fullRowRect.width - (count - 1) * vSpacing) / count;
			Rect rect = new(fullRowRect.x, fullRowRect.y, width, fullRowRect.height);

			foreach (VoxelTool tool in tools)
			{
				DrawVoxelTool(voxelEditor, selectedTool, selectedAction, rect, tool);
				rect.x += width + vSpacing;
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
				: notSelectedButtonStyle;

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
			Rect fullLine = position.SliceOut(_actionButtonHeight);

			int count = VoxelEditor_EnumHelper.allVoxelActions.Length;
			float width = (fullLine.width - (count - 1) * vSpacing) / count;
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

				GUIStyle style = !isSelected ? notSelectedButtonStyle :
					action == VoxelAction.Attach ? selectedButtonAttachStyle :
					action == VoxelAction.Erase ? selectedButtonEraseStyle :
					action == VoxelAction.Overwrite ? selectedButtonOverWriteStyle :
					action == VoxelAction.Repaint ? selectedButtonRecolorStyle : selectedButtonStyle;

				if (GUI.Button(rect, content, style))
					voxelEditor.SelectedAction = action;

				rect.x += width + vSpacing;
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
				voxelEditor.MaterialPalette,
				voxelEditor.SelectedMaterialIndex,
				newSelectedIndex => voxelEditor.SelectedMaterialIndex = newSelectedIndex,
				VoxelTool.MaterialPicker,
				new("Material Index:", "The selected index of the Material palette"),
				ref position);

			DrawPalette(
				voxelEditor,
				voxelEditor.ShapePalette,
				voxelEditor.SelectedShapeIndex,
				newSelectedIndex => voxelEditor.SelectedShapeIndex = newSelectedIndex,
				VoxelTool.ShapePicker,
				new("Shape Index:", "The selected index of the Shape palette"),
				ref position);
			GUI.enabled = tempEnabled;
		}

		const int itemsInARow = 3;
		public static float GetPaletteHeight(IPalette palette)
		{ 
			if (palette == null) return 0;
			int count = palette?.Count ?? 0; 
			int rows = Mathf.CeilToInt(count / (float)itemsInARow);
			return rows * _paletteButtonSize + (rows - 1) * vSpacing;
		}

		static void DrawPalette(
			IVoxelEditor voxelEditor, IPalette palette, int selectedIndex,
			Action<int> onSelect, VoxelTool voxel, GUIContent title, ref Rect position)
		{
			GUI.color = Color.white;
			Rect oneRowRect = position.SliceOutLine();

			Rect rect = oneRowRect;
			const float colorPickerWidth = 30;
			rect.width = colorPickerWidth;
			DrawVoxelTool(voxelEditor, voxelEditor.SelectedTool, voxelEditor.SelectedAction, rect, voxel);

			rect.x += colorPickerWidth + vSpacing;
			rect.width = oneRowRect.width - colorPickerWidth + vSpacing;

			if (selectedIndex > 0 && selectedIndex >= palette.Count)
			{
				title.tooltip = "This index is over the palette's range!";
				title.image = warningIcon;
			}

			int newValue = EditorGUI.IntField(rect, title, selectedIndex);
			newValue = Mathf.Max(newValue, 0);

			int i = 0;
			const float colorSpacing = 4;
			float itemWidth = (oneRowRect.width - (itemsInARow - 1) * vSpacing) / itemsInARow;
			if (palette != null)
				foreach (IPaletteItem item in palette.PaletteItems)
				{
					if (i % itemsInARow == 0)
					{
						oneRowRect = position.SliceOut(_paletteButtonSize);
						oneRowRect.width = itemWidth;
					}

					GUI.color = i == selectedIndex ? Color.white : new Color(1, 1, 1, 0.4f);
					bool click = GUI.Button(oneRowRect, item.DisplayName, paletteButton);
					Rect colorRect = new(oneRowRect.x + colorSpacing, oneRowRect.y + colorSpacing, oneRowRect.width - 2 * colorSpacing, oneRowRect.height - 2 * colorSpacing);
					GUI.color = i == selectedIndex ? Color.white : new Color(1, 1, 1, 0.65f);
					if (item.DisplayColor.a != 0)
					{
						Color c = item.DisplayColor;
						c.a = 1;
						EditorHelper.DrawBox(colorRect, c);
						bool isDark = c.r + c.g + c.b < 1.5f;
						GUI.color = isDark ? Color.white : Color.black;
					}

					GUI.Label(oneRowRect, item.DisplayName, paletteDarkStyle);
					if (click)
						newValue = i;

					oneRowRect.x += (itemWidth + vSpacing);
					i++;
				}


			if (newValue != selectedIndex)
			{
				Undo.RecordObject(voxelEditor.EditorObject, "Selected Value Changed");
				if (voxelEditor.SelectedAction == VoxelAction.Erase)
					voxelEditor.SelectedAction = VoxelAction.Attach;

				onSelect(newValue);
			}
			GUI.color = Color.white;
		}

		public static void DrawExtraControls(VoxelEditor voxelEditor, ref Rect position) 
		{

			VoxelShapeBuilder selectedShape = voxelEditor.SelectedShape;

			IReadOnlyList<ExtraControl> extraControls = selectedShape == null ? null : selectedShape.GetExtraControls();
			if(extraControls == null || extraControls.Count == 0) return;

			int controlCount = extraControls.Count;
				 
			Rect fullRect = position.SliceOut(GetPanelHeight(controlCount));

			Undo.RecordObject(voxelEditor.EditorObject, "Selected Value Changed");
			int voxelData = voxelEditor.SelectedVoxelValue;
			ushort extraVoxelData = voxelData.GetExtraVoxelData();


			foreach (ExtraControl control in extraControls)
			{
				Rect controlRect = fullRect.SliceOutLine();
				EditorGUI.LabelField(controlRect, control.name);
				controlRect = EditorHelper.ContentRect(controlRect);

				object oldValue = control.GetExtraData(extraVoxelData);
				bool isExpanded = true;
				object newValue = EditorHelper.AnythingField(controlRect, control.DataType, oldValue, GUIContent.none, ref isExpanded);

				extraVoxelData = control.SetExtraData(extraVoxelData, newValue);
			}
			voxelData.SetExtraVoxelData(extraVoxelData);
			voxelEditor.SelectedVoxelValue = voxelData; 
		}

		// Voxel Transformation

		public static float GetVoxelTransformationPanelHeight() => GetPanelHeight(2);

		public static float GetPanelHeight(int standardLineCount) => 
			standardLineCount * singleLineHeight + (standardLineCount - 1) * vSpacing;

		/*
		public static void DrawVoxelTransformation(VoxelEditor voxelEditor, ref Rect position, GeneralDirection2D drawTo)
		{
			bool tempEnabled = GUI.enabled;
			GUI.enabled = voxelEditor.IsEditingEnabled();
			int voxelValue = voxelEditor.SelectedVoxelValue;
			Vector3Int rotation = voxelValue.GetRotation();
			Flip3D flip = voxelValue.GetFlip();

			float panelHeight = GetVoxelTransformationPanelHeight();
			Rect fullRect = position.SliceOut(panelHeight, drawTo);
			Rect rotationRect = fullRect.SliceOutLine(); 

			EditorGUI.LabelField(rotationRect, "Rotation");
			rotationRect = EditorHelper.ContentRect(rotationRect);
			Vector3Int newRotation = EditorGUI.Vector3IntField(rotationRect, GUIContent.none, rotation);
			Flip3D newFlip = (Flip3D)EditorGUI.EnumPopup(fullRect.SliceOutLine(), "Flip", flip);

			if (GUI.enabled)
			{
				if (flip != newFlip || rotation != newRotation)
				{ 
					Undo.RecordObject(voxelEditor.EditorObject, "Selected Value Changed");
					voxelEditor.SelectedRotation = newRotation;
					voxelEditor.SelectedFlip = newFlip;
				}
			}
			GUI.enabled = tempEnabled; 
		}
		*/

		// Draw Preview

		static readonly CustomMeshPreview customMeshPreview = new();
		static readonly MeshBuilder previewMeshBuilder = new();
		static Mesh previewMesh;
		static Mesh GetPreviewMesh() => previewMesh;
		static int lastPreviewedShapeIndex = -1;
		static ushort lastExtraVoxelData = 0;

		public static void DrawVoxelPreview(VoxelEditor voxelEditor, ref Rect position, GeneralDirection2D drawTo)
		{
			if (voxelEditor.ShapePalette == null) return;
			if (voxelEditor.SelectedShapeIndex >= voxelEditor.ShapePalette.Shapes.Count) return;
			if (Event.current.type != EventType.Repaint) return;
			
			Rect rect = position.SliceOut(150, drawTo);

			customMeshPreview.TextureSize = new Vector2(rect.width, rect.height);
			customMeshPreview.BackgroundType = CameraClearFlags.Skybox;
			Vector3 cameraEulerAngles = SceneView.lastActiveSceneView.camera.transform.eulerAngles;
			customMeshPreview.CameraAngle = new Vector2(-cameraEulerAngles.y, cameraEulerAngles.x);
			customMeshPreview.Material = voxelEditor.SelectedMaterial?.Material;
			customMeshPreview.meshGetter = GetPreviewMesh;
			customMeshPreview.SetDirty();

			int shapeIndex = voxelEditor.SelectedShapeIndex;
			ushort extraVoxelData = voxelEditor.SelectedVoxelValue.GetExtraVoxelData();
 
			if (shapeIndex != lastPreviewedShapeIndex || lastExtraVoxelData != extraVoxelData )
			{
				lastPreviewedShapeIndex = shapeIndex;
				lastExtraVoxelData = extraVoxelData;

				VoxelShapeBuilder shape = voxelEditor.ShapePalette.Shapes[voxelEditor.SelectedShapeIndex];
				ArrayVoxelMap map = new (Vector3Int.one);
				map.SetVoxel(Vector3Int.zero, voxelEditor.SelectedVoxelValue);
				previewMeshBuilder.Clear();
				shape.GenerateMeshData(map, new() { Vector3Int.zero }, 0, previewMeshBuilder, false);

				if (previewMesh == null)
					previewMesh = new Mesh();
				else
					previewMesh.Clear();

				previewMeshBuilder.CopyToMesh(previewMesh);
			}

			EditorGUI.DrawPreviewTexture(rect, customMeshPreview.PreviewTexture);
		}
	}
}
#endif