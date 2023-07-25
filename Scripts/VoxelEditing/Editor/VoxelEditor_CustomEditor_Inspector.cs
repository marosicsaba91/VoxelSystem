#if UNITY_EDITOR
using MUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	[CustomEditor(typeof(VoxelEditor))]
	partial class VoxelEditor_CustomEditor : Editor
	{
		static VoxelEditorSettings iconSettings;
		static Dictionary<VoxelAction, GUIContent> _actionToContent = new();
		static Dictionary<VoxelTool, GUIContent> _toolToContent = new();
		static Dictionary<(VoxelTool, VoxelAction), GUIContent> _toolWithActionToContent = new();

		static float _spacing;
		const int _actionButtonHeight = 20;  // 16+4
		const int _toolButtonHeight = 36;    // 32 + 4

		static readonly VoxelTool[] transformTools =
			VoxelEditor_EnumHelper._allVoxelTools.Where(t => t.IsTransformTool()).ToArray();

		static readonly VoxelTool[] cursorTools =
			VoxelEditor_EnumHelper._allVoxelTools.Where(t => t.IsCursorTool()).ToArray();

		static Texture warningIcon;
		static GUIStyle headerStyle;
		static GUIStyle paletteStyle;

		static GUIStyle notSelectedButtonStyle;
		static GUIStyle selectedButtonStyle;
		static GUIStyle selectedButtonAttachStyle;
		static GUIStyle selectedButtonEraseStyle;
		static GUIStyle selectedButtonOverWriteStyle;
		static GUIStyle selectedButtonRecolorStyle;


		GUIStyle GetSelectedButtonStyle(VoxelAction current, VoxelAction[] supported)
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

		void OnEnable()
		{
			SetupSerializedProperties();
		}


		void SetupSerializedProperties()
		{
			// transformLockProperty = serializedObject.FindProperty(nameof(VoxelEditor.transformLock));
		}

		static void SetupGuiContentAndStyle()
		{
			iconSettings = VoxelEditorSettings.Instance;
			if (headerStyle == null)
			{
				try
				{
					headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
					paletteStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };

					notSelectedButtonStyle = new GUIStyle(GUI.skin.button);
					selectedButtonStyle = new GUIStyle(GUI.skin.button);
					selectedButtonAttachStyle = new GUIStyle(GUI.skin.button);
					selectedButtonEraseStyle = new GUIStyle(GUI.skin.button);
					selectedButtonOverWriteStyle = new GUIStyle(GUI.skin.button);
					selectedButtonRecolorStyle = new GUIStyle(GUI.skin.button);
					selectedButtonStyle.normal.background = iconSettings.selectedButton;
					selectedButtonAttachStyle.normal.background = iconSettings.selectedButtonAttach;
					selectedButtonEraseStyle.normal.background = iconSettings.selectedButtonErase;
					selectedButtonOverWriteStyle.normal.background = iconSettings.selectedButtonOverWrite;
					selectedButtonRecolorStyle.normal.background = iconSettings.selectedButtonRecolor;
				}
				catch (NullReferenceException) { }
			}

			warningIcon = EditorGUIUtility.IconContent("Warning").image;
			_spacing = EditorGUIUtility.standardVerticalSpacing;

			if (!_actionToContent.IsNullOrEmpty() && !_toolWithActionToContent.IsNullOrEmpty())
				return;

			iconSettings = VoxelEditorSettings.Instance;

			_actionToContent = new Dictionary<VoxelAction, GUIContent>();
			_toolToContent = new Dictionary<VoxelTool, GUIContent>();
			_toolWithActionToContent = new Dictionary<(VoxelTool, VoxelAction), GUIContent>();

			foreach (VoxelTool tool in VoxelEditor_EnumHelper._allVoxelTools)
			{
				Texture texture = iconSettings.GetToolIcon(tool);
				if (texture != null)
				{
					string toolTip = tool.ToString();
					GUIContent content = new(texture, toolTip);
					_toolToContent.Add(tool, content);
				}
			}

			foreach (VoxelAction action in VoxelEditor_EnumHelper._allVoxelActions)
			{
				Texture texture = iconSettings.GetActionIcon(action);
				GUIContent content = new(texture, action.GetTooltip());
				_actionToContent.Add(action, content);

				foreach (VoxelTool tool in VoxelEditor_EnumHelper._allVoxelTools)
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

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			VoxelEditor voxelEditor = GetTarget();
			if (voxelEditor == null) return;

			SetupGuiContentAndStyle();
			SetupSerializedProperties();

			VoxelAction selectedAction = voxelEditor.SelectedAction;
			VoxelTool selectedTool = voxelEditor.SelectedTool;
			TransformLock transformLock = voxelEditor.transformLock;

			UpdateEnableEdit(voxelEditor);
			GUI.enabled = _enableEdit;

			if (voxelEditor.Map == null)
			{
				EditorGUILayout.HelpBox("No Map to Edit. Use VoxelFilter", UnityEditor.MessageType.Warning);
				return;
			}

			DrawTitle(voxelEditor);
			DrawMapCommands(voxelEditor);
			DrawTransformLocks(transformLock, voxelEditor);
			EditorGUILayout.Space();
			DrawVoxelActions(voxelEditor, selectedAction, selectedTool);
			DrawToolRow(voxelEditor, cursorTools, selectedTool, selectedAction);
			DrawToolRow(voxelEditor, transformTools, selectedTool, selectedAction);
			EditorGUILayout.Space();
			DrawSelectionTools(voxelEditor);
			EditorGUILayout.Space();
			DrawVoxelPalette(voxelEditor);

			serializedObject.ApplyModifiedProperties();
		}

		void DrawTitle(VoxelEditor voxelEditor)
		{
			EditorGUILayout.LabelField("Map Name:   " + voxelEditor.MapName, headerStyle);
			EditorGUILayout.Space(-5);
			EditorGUILayout.LabelField(voxelEditor.Map.FullSize.ToString(), EditorStyles.centeredGreyMiniLabel);
		}

		void DrawMapCommands(IVoxelEditor voxelEditor)
		{
			float height = EditorGUIUtility.singleLineHeight;
			Rect position = EditorGUILayout.GetControlRect(false, height);
			int count = 3;
			float width = (position.width - (count - 1) * _spacing) / count;
			position.width = width;

			bool change = false;
			position.height = height;
			if (GUI.Button(position, "Clear Map"))
			{
				voxelEditor.RecordForUndo("Map Cleared", RecordType.Map);
				change = voxelEditor.Map.ClearWhole();
			}

			position.x += width + _spacing;
			if (GUI.Button(position, "Fill Map"))
			{
				voxelEditor.RecordForUndo("Map Filled", RecordType.Map);
				change = voxelEditor.Map.SetWhole(voxelEditor.SelectedPaletteIndex);
			}
			position.x += width + _spacing;
			GUI.enabled = VoxelClipboard.HaveContent && _enableEdit;
			if (GUI.Button(position, "Setup from Clipboard"))
			{
				voxelEditor.RecordForUndo("Selection Cleared", RecordType.Map | RecordType.Transform | RecordType.Editor);
				voxelEditor.Map.SetupFrom(VoxelClipboard.ClipboardMap);
				if (voxelEditor.HasSelection())
					voxelEditor.transform.position += voxelEditor.Selection.min;
				voxelEditor.Deselect();
				change |= true;
			}
			GUI.enabled = _enableEdit;

			if (change)
				voxelEditor.Map.MapChanged(MapChange.Final);
		}

		void DrawSelectionTools(IVoxelEditor voxelEditor)
		{
			float height = EditorGUIUtility.singleLineHeight;
			Rect position = EditorGUILayout.GetControlRect(false, height * 4 + 3 * _spacing);
			int count = 3;
			float width = (position.width - (count - 1) * _spacing) / count;
			Rect buttonRect = position;
			buttonRect.width = width;

			buttonRect.height = height * 2 + _spacing;
			DrawVoxelTool(voxelEditor, voxelEditor.SelectedTool, voxelEditor.SelectedAction, buttonRect, VoxelTool.Select);
			buttonRect.x += width + _spacing;

			bool change = false;
			buttonRect.height = height;

			Vector3Int fullMapSize = voxelEditor.Map.FullSize;
			GUI.enabled = _enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, " De-Select"))
			{
				voxelEditor.RecordForUndo("Remove Selection", RecordType.Editor);
				voxelEditor.Deselect();
			}
			buttonRect.x += width + _spacing;
			GUI.enabled = _enableEdit && voxelEditor.Selection.size != fullMapSize;
			if (GUI.Button(buttonRect, " SelectAll"))
			{
				voxelEditor.RecordForUndo("Remove Selection", RecordType.Editor);
				voxelEditor.Selection = new BoundsInt(Vector3Int.zero, fullMapSize);
			}
			GUI.enabled = _enableEdit;

			buttonRect.x -= width + _spacing;
			buttonRect.y += height + _spacing;
			GUI.enabled = _enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, " Copy"))
			{
				voxelEditor.Copy();
			}
			buttonRect.x += width + _spacing;
			GUI.enabled = _enableEdit && VoxelClipboard.HaveContent;
			if (GUI.Button(buttonRect, " Paste"))
			{
				voxelEditor.RecordForUndo("Voxel Selection Pasted", RecordType.Map | RecordType.Editor);
				voxelEditor.Paste();
				change = true;
			}
			 
			width = (position.width - _spacing) / 2;
			buttonRect = position;
			buttonRect.y += height * 2 + 2 * _spacing;
			buttonRect.height = height;
			buttonRect.width = width;

			GUI.enabled = _enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, "Clear Selection"))
			{
				voxelEditor.RecordForUndo("Selection Cleared", RecordType.Map);
				change = voxelEditor.ClearInsideSelection();
			}

			buttonRect.x += width + _spacing;
			GUI.enabled = _enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, "Fill Selection"))
			{
				voxelEditor.RecordForUndo("Selection Filled", RecordType.Map);
				change = voxelEditor.FillInsideSelection();
			}

			buttonRect.y += height + _spacing;
			buttonRect.x -= width + _spacing;

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
			buttonRect.x += width + _spacing;
			GUI.enabled = _enableEdit && voxelEditor.HasSelection();
			if (GUI.Button(buttonRect, "Separate Selection"))
			{
				voxelEditor.SeparateSelectionToGameObject();
				change = true;
			}
			GUI.enabled = _enableEdit;

			if (change)
				voxelEditor.Map.MapChanged(MapChange.Final);
		}

		void DrawTransformLocks(TransformLock tLock, VoxelEditor voxelEditor)
		{
			Rect position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

			int count = 3;
			float width = (position.width - (count - 1) * _spacing) / count;
			Rect rect = new(position.x, position.y, width, position.height);
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
			GUI.enabled = _enableEdit && tLock.rotation && tLock.scale;
			rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
			GUIContent content = new("Apply Transform to Map", "Apply Transform rotation & scale to the Map (Need to lock rotation and scale)");
			if (GUI.Button(rect, content))
			{
				VoxelMap map = voxelEditor.Map;
				Transform transform = voxelEditor.transform;
				voxelEditor.RecordForUndo("Rotation & Scale Applied to Map", RecordType.Map | RecordType.Transform);
				map.ApplyScale(transform);
				map.ApplyRotation(transform);
			}
			GUI.enabled = _enableEdit;


			bool DrawOneLock(bool b, string text)
			{

				GUIContent cont = new(text, b ? iconSettings.lockOnIcon : iconSettings.lockOffIcon, "Locking Transform " + text + " to whole values");
				GUI.color = b ? new Color(0.75f, 0.75f, 0.75f) : Color.white;
				if (GUI.Button(rect, cont))
					b = !b;
				rect.x += width + _spacing;
				return b;
			}
		}

		void DrawToolRow(IVoxelEditor voxelEditor, VoxelTool[] tools, VoxelTool selectedTool, VoxelAction selectedAction)
		{
			Rect position = EditorGUILayout.GetControlRect(false, _toolButtonHeight);

			int count = tools.Length;
			float width = (position.width - (count - 1) * _spacing) / count;
			Rect rect = new(position.x, position.y, width, position.height);

			foreach (VoxelTool tool in tools)
			{
				DrawVoxelTool(voxelEditor, selectedTool, selectedAction, rect, tool);
				rect.x += width + _spacing;
			}
			GUI.color = Color.white;
		}


		void DrawVoxelTool(IVoxelEditor voxelEditor, VoxelTool selectedTool, VoxelAction selectedAction, Rect rect, VoxelTool drawnTool)
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


		void DrawVoxelActions(IVoxelEditor voxelEditor, VoxelAction selectedAction, VoxelTool selectedTool)
		{
			Rect position = EditorGUILayout.GetControlRect(false, _actionButtonHeight);

			int count = VoxelEditor_EnumHelper._allVoxelActions.Length;
			float width = (position.width - (count - 1) * _spacing) / count;
			Rect rect = new(position.x, position.y, width, position.height);

			VoxelAction[] supportedActions = selectedTool == VoxelTool.None
				? VoxelEditor_EnumHelper._allVoxelActions
				: selectedTool.GetHandler().GetSupportedActions(voxelEditor);

			foreach (VoxelAction action in VoxelEditor_EnumHelper._allVoxelActions)
			{
				GUIContent content = _actionToContent[action];
				bool isActionEnabled = supportedActions.Contains(action);
				bool isSelected = selectedAction == action;

				GUI.enabled = _enableEdit && isActionEnabled;

				GUIStyle style = !isSelected ? notSelectedButtonStyle :
					action == VoxelAction.Attach ? selectedButtonAttachStyle :
					action == VoxelAction.Erase ? selectedButtonEraseStyle :
					action == VoxelAction.Overwrite ? selectedButtonOverWriteStyle :
					action == VoxelAction.Repaint ? selectedButtonRecolorStyle : selectedButtonStyle;

				if (GUI.Button(rect, content, style))
					voxelEditor.SelectedAction = action;

				rect.x += width + _spacing;

			}

			GUI.enabled = _enableEdit;
			GUI.backgroundColor = Color.white;
		}

		void DrawVoxelPalette(IVoxelEditor voxelEditor)
		{
			Rect position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

			Rect rect = position;
			const float colorPickerWidth = 20;
			rect.width = colorPickerWidth; 
			DrawVoxelTool(voxelEditor, voxelEditor.SelectedTool, voxelEditor.SelectedAction, rect, VoxelTool.ColorPicker);


			rect.x += colorPickerWidth + _spacing;
			rect.width = position.width - colorPickerWidth + _spacing;

			int paletteLength = voxelEditor.PaletteLength;
			int selected = voxelEditor.SelectedPaletteIndex;

			// IntField Flexing
			GUIContent content = (selected > 0 && selected >= paletteLength)
				? new("Voxel Material Index:", warningIcon, "This index is over the current voxel palette's range!")
				: new("Color Index:", "The index of the voxel type in the voxel palette");

			int newValue = EditorGUI.IntField(rect, content, selected);
			newValue = Mathf.Max(newValue, 0);




			int i = 0;
			const int itemsInARow = 6;
			const float colorSpacing = 6;
			float itemWidth = (position.width - (itemsInARow - 1) * _spacing) / itemsInARow;
			foreach (IVoxelPaletteItem item in voxelEditor.PaletteItems)
			{
				if (i % itemsInARow == 0)
				{
					position = EditorGUILayout.GetControlRect(false, _toolButtonHeight);
					position.width = itemWidth;
				}

				GUI.color = i == selected ? Color.white : new Color(1, 1, 1, 0.5f);
				bool click = GUI.Button(position, item.Name);
				Rect colorRect = new(position.x + colorSpacing, position.y + colorSpacing, position.width - 2 * colorSpacing, position.height - 2 * colorSpacing);
				GUI.color = i == selected ? Color.white : new Color(1, 1, 1, 0.65f);
				EditorHelper.DrawBox(colorRect, item.Color);
				GUI.color = Color.Lerp(Color.black, item.Color, 0.25f);
				GUI.Label(position, item.Name, paletteStyle);
				if (click)
					newValue = i;

				position.x += (itemWidth + _spacing);
				i++;
			}


			if (newValue != selected)
			{
				Undo.RecordObject(voxelEditor.EditorObject, "Selected Voxel Type Changed");
				if (voxelEditor.SelectedAction == VoxelAction.Erase)
					voxelEditor.SelectedAction = VoxelAction.Attach;
				voxelEditor.SelectedPaletteIndex = newValue;
			}
		}
	}

}

#endif