#if UNITY_EDITOR
using MUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace VoxelSystem
{
	[CustomEditor(typeof(VoxelEditor))]
	partial class VoxelEditor_CustomEditor : Editor
	{
		SerializedProperty selectedActionProperty;
		SerializedProperty selectedToolProperty;
		SerializedProperty selectedPaletteIndexProperty;
		SerializedProperty transformLockProperty;

		static VoxelEditorSettings iconSettings;
		static readonly Color notSelectedColor = new Color(0.75f, 0.75f, 0.75f);
		static Dictionary<VoxelAction, GUIContent> _actionToContent = new();
		static Dictionary<(VoxelTool, VoxelAction), GUIContent> _toolToContent = new();

		static float _spacing;
		const int _actionButtonHeight = 20;  // 16+4
		const int _toolButtonHeight = 36;    // 32 + 4

		static readonly VoxelTool[] transformTools =
			VoxelEditor_EnumHelper._allVoxelTools.Where(t => t.IsTransformTool()).ToArray();

		static readonly VoxelTool[] cursorTools =
			VoxelEditor_EnumHelper._allVoxelTools.Where(t => t.IsCursorTool()).ToArray();

		static Texture warningIcon;
		static GUIStyle headerStyle;

		void OnEnable()
		{
			warningIcon = EditorGUIUtility.IconContent("Warning").image;
			_spacing = EditorGUIUtility.standardVerticalSpacing;

			selectedActionProperty = serializedObject.FindProperty(nameof(VoxelEditor.selectedAction));
			selectedToolProperty = serializedObject.FindProperty(nameof(VoxelEditor.selectedTool));
			selectedPaletteIndexProperty = serializedObject.FindProperty(nameof(VoxelEditor.selectedPaletteIndex));
			transformLockProperty = serializedObject.FindProperty(nameof(VoxelEditor.transformLock));

			SetupGuiContent();
		}

		static void SetupGuiContent()
		{
			if (headerStyle == null)
			{
				try
				{
					headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
				}
				catch (NullReferenceException) { }
			}


			if (!_actionToContent.IsNullOrEmpty() && !_toolToContent.IsNullOrEmpty())
				return;

			iconSettings = VoxelEditorSettings.Instance;

			_actionToContent = new Dictionary<VoxelAction, GUIContent>();
			_toolToContent = new Dictionary<(VoxelTool, VoxelAction), GUIContent>();

			foreach (VoxelAction action in VoxelEditor_EnumHelper._allVoxelActions)
			{
				Texture texture = iconSettings.GetActionIcon(action);
				GUIContent content = new(texture, action.ToString());
				_actionToContent.Add(action, content);
				foreach (VoxelTool tool in VoxelEditor_EnumHelper._allVoxelTools)
				{
					texture = iconSettings.GetToolIcon(tool, action);
					string toolTip = tool.IsCursorTool()
						? tool.ToString() + " - " + action.ToString()
						: tool.ToString();
					GUIContent content2 = new(texture, toolTip);
					_toolToContent.Add((tool, action), content2);
				}
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var voxelEditor = (target as VoxelEditor);

			VoxelAction selectedAction = voxelEditor.SelectedAction;
			VoxelTool selectedTool = voxelEditor.selectedTool;
			int selectedPaletteIndex = voxelEditor.selectedPaletteIndex;
			TransformLock transformLock = voxelEditor.transformLock;

			SetupGuiContent();

			if (voxelEditor.Map == null)
			{
				EditorGUILayout.HelpBox("No Map To Edit. Use VoxelFilter", UnityEditor.MessageType.Warning);
				return;
			}

			DrawTitle(voxelEditor);

			//EditorGUILayout.LabelField("Transform Tools");
			EditorGUILayout.Space();
			DrawToolRow(transformTools, selectedTool, selectedAction);
			DrawTransformLocks(transformLock, voxelEditor);
			//EditorGUILayout.LabelField("Edit Tool Tools");
			EditorGUILayout.Space();
			DrawToolRow(cursorTools, selectedTool, selectedAction);
			DrawVoxelActions(voxelEditor, selectedAction, selectedTool);
			//EditorGUILayout.LabelField("Selection Tools");
			EditorGUILayout.Space();
			DrawSelectionTools(voxelEditor);
			//EditorGUILayout.LabelField("Voxel Palette");
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

		void DrawSelectionTools(IVoxelEditor voxelEditor)
		{
			float height = EditorGUIUtility.singleLineHeight;
			Rect position = EditorGUILayout.GetControlRect(false, height*2 + _spacing); 
			int count = 3;
			float width = (position.width - (count - 1) * _spacing) / count;
			position.width = width;


			DrawVoxelTool(voxelEditor.SelectedTool, voxelEditor.SelectedAction, position, VoxelTool.Select);
			position.x += width + _spacing;

			bool change = false;
			position.height = height;
			string text = voxelEditor.HasSelection() ? "Clear Selection" : "Clear Map";
			if (GUI.Button(position, text))
			{
				voxelEditor.RecordForUndo("Selection Cleared", RecordType.Map);
				change |= voxelEditor.ClearSelection();
			}

			position.x += width + _spacing;
			text = voxelEditor.HasSelection() ? "Fill Selection" : "Fill Map";
			if (GUI.Button(position, text))
			{
				voxelEditor.RecordForUndo("Selection Cleared", RecordType.Map);
				change |= voxelEditor.FillSelection();
			}

			position.x -= width + _spacing;
			position.width += width + _spacing;
			position.y += height + _spacing;
			GUI.enabled = voxelEditor.HasSelection();
			if (GUI.Button(position, " De-Select"))
			{
				voxelEditor.RecordForUndo("Remove Selection", RecordType.Editor);
				voxelEditor.Deselect();
			}
			GUI.enabled = true;

			if (change)
				voxelEditor.Map.MapChanged();
		}

		void DrawTransformLocks(TransformLock tLock, VoxelEditor voxelEditor)
		{
			Rect position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

			int count = 6;
			float width = (position.width - (count - 1) * _spacing) / count;
			var rect = new Rect(position.x, position.y, width, position.height);
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
			GUI.enabled = voxelEditor.EnableEdit && tLock.rotation && tLock.scale;
			rect.width = width * 3 + 2 * _spacing;
			GUIContent content = new("Apply Transform to Map", "Apply Transform rotation & scale to the Map (Need to lock rotation and scale)");
			if (GUI.Button(rect, content))
			{
				VoxelMap map = voxelEditor.Map;
				Transform transform = voxelEditor.transform;
				voxelEditor.RecordForUndo("Rotation & Scale Applied to Map", RecordType.Map | RecordType.Transform);
				map.ApplyScale(transform);
				map.ApplyRotation(transform);
			}
			GUI.enabled = voxelEditor.EnableEdit;


			bool DrawOneLock(bool b, string text)
			{

				GUIContent cont = new(null, b ? iconSettings.lockOnIcon : iconSettings.lockOffIcon, "Locking Transform " + text + " to whole values");
				GUI.color = b ? notSelectedColor : Color.white;
				if (GUI.Button(rect, cont))
					b = !b;
				rect.x += width + _spacing;
				return b;
			}
		}

		void DrawToolRow(VoxelTool[] tools, VoxelTool selectedTool, VoxelAction selectedAction)
		{
			Rect position = EditorGUILayout.GetControlRect(false, _toolButtonHeight);

			int count = tools.Length;
			float width = (position.width - (count - 1) * _spacing) / count;
			var rect = new Rect(position.x, position.y, width, position.height);

			foreach (VoxelTool tool in tools)
			{
				DrawVoxelTool(selectedTool, selectedAction, rect, tool);
				rect.x += width + _spacing;
			}
			GUI.color = Color.white;
		}

		void DrawVoxelTool(VoxelTool selectedTool, VoxelAction selectedAction, Rect rect, VoxelTool drawnTool)
		{
			GUIContent content = _toolToContent[(drawnTool, selectedAction)];
			GUI.color = selectedTool == drawnTool ? notSelectedColor : Color.white;
			if (GUI.Button(rect, content))
			{
				selectedToolProperty.enumValueIndex = selectedTool == drawnTool
					? (int)VoxelTool.None
					: (int)drawnTool;
			}
		}

		void DrawVoxelActions(IVoxelEditor voxelEditor, VoxelAction selectedAction, VoxelTool selectedTool)
		{
			Rect position = EditorGUILayout.GetControlRect(false, _actionButtonHeight);
			 
			int count = VoxelEditor_EnumHelper._allVoxelActions.Length;
			float width = (position.width - (count - 1) * _spacing) / count;
			var rect = new Rect(position.x, position.y, width, position.height);

			VoxelAction[] enabledTools = selectedTool == VoxelTool.None
				? new VoxelAction[0]
				: selectedTool.GetHandler().SupportedActions;

			foreach (VoxelAction action in VoxelEditor_EnumHelper._allVoxelActions)
			{
				GUIContent content = _actionToContent[action]; 
				GUI.enabled = enabledTools.Contains(action);
				GUI.color = selectedAction == action ? notSelectedColor : Color.white;
				if (GUI.Button(rect, content))
					selectedActionProperty.enumValueIndex = (int)action;
				rect.x += width + _spacing;
			}

			GUI.enabled = true;
			GUI.color = Color.white; 
		}


		private void DrawVoxelPalette(IVoxelEditor voxelEditor)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, _actionButtonHeight);

			VoxelPalette palette = voxelEditor.VoxelPalette;
			int paletteLength = palette != null ? palette.Length : 0;
			int selected = voxelEditor.SelectedPaletteIndex;

			// Set the length of label
			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = rect.width - 40f;

			// IntField Flexing
			GUIContent content = (selected > 0 && selected >= paletteLength)
				? new("Color Index:", warningIcon, "This index is over the current voxel palette's range!")
				: new("Color Index:", "The index of the voxel type in the voxel palette");

			int newValue = EditorGUI.IntField(rect, content, selected);
			newValue = Mathf.Max(newValue, 0);

			rect.height = EditorGUIUtility.singleLineHeight;
			if (newValue != selected)
			{
				Undo.RecordObject(voxelEditor.EditorObject, "Selected Voxel Type Changed"); 
				voxelEditor.SelectedPaletteIndex = newValue;
			}

			EditorGUIUtility.labelWidth = labelWidth;
		}
	}
}

#endif