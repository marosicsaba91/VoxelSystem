#if UNITY_EDITOR
using MUtility;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace VoxelSystem
{
	public partial class VoxelEditorWindow
	{
		// Add menu named "Voxel Editor" to the Tools menu
		[MenuItem("Tools/Voxel Editor")]
		static void Init()
		{
			// Get existing open window or if none, make a new one:
			VoxelEditorWindow window = (VoxelEditorWindow)GetWindow(typeof(VoxelEditorWindow));
			window.Show();
		}

		const float bigSpacing = 5f;
		const float smallSpacing = 2f;
		const int columnCount = 3;

		GUIStyle _boldMiddleTextStyle;
		Font _normalFont;

		void OnGUI()
		{
			SetupStyles();

			if (_editorComponent == null)
			{
				GUI.Label(position, "No VoxelObject Selected!\nSelect a VoxelObject to edit!", _boldMiddleTextStyle);
				return;
			}

			FindRects(out Rect mapRect, out Rect transformRect, out Rect editRect, out Rect paletteRect);

			GUI.enabled = _editorComponent.EnableEdit;
			if (!_editorComponent.EnableEdit)
				_editorComponent.SelectedTool = VoxelTool.None;

			DrawMapTools(mapRect);

			DrawTransform(transformRect);
			DrawEditTools(editRect);
			DrawPalette(paletteRect, _editorComponent);
		}

		void SetupStyles()
		{
			_normalFont = GUI.skin.font;
			_boldMiddleTextStyle = new(EditorStyles.boldLabel)
			{
				alignment = TextAnchor.MiddleCenter
			};
		}

		void FindRects(out Rect mapRect, out Rect transformRect, out Rect editRect, out Rect paletteRect)
		{
			Rect pos = new(bigSpacing, bigSpacing, position.width - bigSpacing * 2, position.height - bigSpacing * 2);

			float columnWidth = pos.width;
			float columnHeight = 100;

			bool horizontalLayout = pos.height < columnWidth;
			if (horizontalLayout)
			{
				columnWidth -= (columnCount - 1) * bigSpacing;
				columnWidth /= columnCount;

				mapRect = new(pos.x, pos.y, columnWidth, columnHeight);
				transformRect = new(columnWidth + bigSpacing * 2, pos.y, columnWidth, columnHeight);
				editRect = new(columnWidth * 2 + bigSpacing * 3, pos.y, columnWidth, columnHeight);
				paletteRect = new(pos.x, columnHeight + bigSpacing * 2, pos.width, pos.height - columnHeight - bigSpacing);
			}
			else
			{
				mapRect = new(pos.x, pos.y, columnWidth, columnHeight);
				transformRect = new(pos.x, columnHeight + bigSpacing, columnWidth, columnHeight);
				editRect = new(pos.x, (columnHeight + bigSpacing) * 2, columnWidth, columnHeight);

				float startY = columnHeight * 3 + bigSpacing * 3;
				paletteRect = new(pos.x, startY, columnWidth, pos.height - startY + bigSpacing);
			}
		}

		void DrawMapTools(Rect rect)
		{
			const int lineCount = 5;
			float lineHeight = (rect.height - (lineCount - 1) * smallSpacing) / lineCount;
			rect.height = lineHeight;

			string label = "Map: " + _editorComponent.MapName;

			GUI.Label(rect, label, _boldMiddleTextStyle);
			Vector3Int s = _editorComponent.Map.FullSize;
			GUIStyle small = new(EditorStyles.centeredGreyMiniLabel);
			rect.y += lineHeight + smallSpacing;
			GUI.Label(rect, " (" + s.x + "x" + s.y + "x" + s.z + ")", small);

			rect.y += lineHeight + smallSpacing;
			DrawCommandLine(rect);
		}

		public static readonly VoxelTool[] _allTransformTools =
			Enum.GetValues(typeof(VoxelTool)).Cast<VoxelTool>().Where(t => t.IsTransformTool()).ToArray();

		public static readonly VoxelTool[] _allPoseTools =
			Enum.GetValues(typeof(VoxelTool)).Cast<VoxelTool>().Where(t => t.IsPoseTool()).ToArray();

		public static readonly VoxelTool[] _allSizeTools =
			Enum.GetValues(typeof(VoxelTool)).Cast<VoxelTool>().Where(t => t.IsSizeTool()).ToArray();

		public static readonly VoxelTool[] _allCursor =
			Enum.GetValues(typeof(VoxelTool)).Cast<VoxelTool>().Where(t => t.IsCursorTool()).ToArray();

		void DrawTransform(Rect rect)
		{
			const int lineCount = 5;
			float lineHeight = (rect.height - (lineCount - 1) * smallSpacing) / lineCount;
			rect.height = lineHeight;
			GUI.Label(rect, "Transform", _boldMiddleTextStyle);
			rect.y += lineHeight + smallSpacing;
			DrawToolLine(rect, _allPoseTools);
			rect.y += lineHeight + smallSpacing;
			DrawToolLine(rect, _allSizeTools);
			rect.y += lineHeight + smallSpacing;
			DrawTransformLocks(rect);
			rect.y += lineHeight + smallSpacing;
			DrawTransformApplyButtons(rect);
		}

		void DrawTransformLocks(Rect rect)
		{
			GUI.Label(rect, "Lock");
			const float labelWidth = 40;

			float buttonWidth = (rect.width - labelWidth - (smallSpacing * 2)) / 3f;
			rect.x += labelWidth;
			rect.width = buttonWidth;

			TransformLock tLock = _editorComponent.TransformLock;
			tLock.position = ToggleButton(rect, "Position", tLock.position);
			rect.x += buttonWidth + smallSpacing;
			tLock.rotation = ToggleButton(rect, "Rotation", tLock.rotation);
			rect.x += buttonWidth + smallSpacing;
			tLock.scale = ToggleButton(rect, "Scale", tLock.scale);
			_editorComponent.TransformLock = tLock;

			bool ToggleButton(Rect r, string label, bool value)
			{
				int textLength = _normalFont.GetTextLength(label);
				if (textLength > r.width - 12)
					label = label[0..3];
				textLength = _normalFont.GetTextLength(label);
				if (textLength > r.width - 10)
					label = label[0..1];

				GUI.backgroundColor = value ? Color.gray : Color.white;
				if (GUI.Button(rect, label))
					value = !value;
				GUI.backgroundColor = Color.white;
				return value;
			}
		}

		void DrawTransformApplyButtons(Rect rect)
		{
			TransformLock tLock = _editorComponent.TransformLock;
			GUI.enabled = _editorComponent.EnableEdit && tLock.rotation && tLock.scale;
			if (GUI.Button(rect, "Apply Scale & Rotation"))
			{
				VoxelMap map = _editorComponent.Map;
				Transform transform = _editorComponent.transform;
				const RecordType recordType = RecordType.Map | RecordType.Transform;
				_editorComponent.RecordForUndo( "Rotation Applied to Map", recordType);
				map.ApplyScale(transform);
				map.ApplyRotation(transform);
			}

			GUI.enabled = _editorComponent.EnableEdit;
		}
		 

		void DrawEditTools(Rect rect)
		{
			const int lineCount = 5;
			float lineHeight = (rect.height - (lineCount - 1) * smallSpacing) / 4;
			rect.height = lineHeight;
			GUI.Label(rect, "Edit", _boldMiddleTextStyle);
			rect.y += lineHeight + smallSpacing;
			DrawActionLine(rect, VoxelEditor_EnumHelper._allVoxelActions);
			GUI.enabled = _editorComponent.EnableEdit;
			rect.y += lineHeight + smallSpacing;
			rect.height += lineHeight + smallSpacing;
			DrawToolLine(rect, _allCursor);
		}


		void DrawToolLine(Rect rect, VoxelTool[] tools)
		{
			if (tools == null || tools.Length == 0)
				return;

			float buttonWidth = (rect.width - (tools.Length - 1) * smallSpacing) / tools.Length;
			rect.width = buttonWidth;

			foreach (VoxelTool t in tools)
			{
				DrawToolButton(rect, t);
				rect.x += buttonWidth + smallSpacing;
			}
		}

		void DrawActionLine(Rect rect, VoxelAction[] actions)
		{
			if (actions == null || actions.Length == 0)
				return;
			 
			float buttonWidth = (rect.width - (actions.Length - 1) * smallSpacing) / actions.Length;
			rect.width = buttonWidth;

			foreach (VoxelAction voxelAction in actions)
			{ 
				DrawActionButton(rect, voxelAction);
				rect.x += buttonWidth + smallSpacing;
			}
		}

		void DrawToolButton(Rect rect, VoxelTool buttonTool)
		{
			GUI.backgroundColor = buttonTool == Tool ? Color.gray : Color.white;

			if (GUI.Button(rect, buttonTool.ToString()))
			{
				if (Tool != buttonTool)
				{
					Tools.hidden = true;
					_editorComponent.SelectedTool = buttonTool;
				}
				else
				{
					Tools.hidden = false;
					_editorComponent.SelectedTool = VoxelTool.None;
				}
			}

			GUI.backgroundColor = Color.white;
		}

		void DrawActionButton(Rect rect, VoxelAction buttonAction)
		{
			GUI.backgroundColor = buttonAction == VoxelActon ? Color.gray : Color.white;

			if (GUI.Button(rect, buttonAction.ToString()))
			{
				Tools.hidden = true;
				_editorComponent.SelectedAction = buttonAction;
			}

			GUI.backgroundColor = Color.white;
		}


		void DrawCommandLine(Rect rect)
		{
			int lineButtonCount = 2;
			float buttonWidth = (rect.width - (lineButtonCount - 1) * smallSpacing) / lineButtonCount;
			rect.width = buttonWidth;


			GUI.enabled = _editorComponent.EnableEdit;
			VoxelMap map = _editorComponent.Map;
			bool change = false;


			string text = _editorComponent.HasSelection() ? "Clear Selection" : "Clear Map";
			if (GUI.Button(rect, text))
			{
				_editorComponent.RecordForUndo("Selection Cleared", RecordType.Map);
				change = _editorComponent.ClearInsideSelection();
			}
			rect.x += buttonWidth + smallSpacing;

			text = _editorComponent.HasSelection() ? "Fill Selection" : "Fill Map";
			if (GUI.Button(rect, text))
			{
				_editorComponent.RecordForUndo("Selection Filled", RecordType.Map);
				change = _editorComponent.FillInsideSelection();
			}

			if (change)
				map.MapChanged();
			
			GUI.enabled = _editorComponent.EnableEdit;
		}
		 

		void DrawPalette(Rect rect, IVoxelEditor voxelEditor)
		{
			GUI.enabled = _editorComponent.EnableEdit && Tool.IsCursorTool();

			const float minWidth = 50;
			const float height = 40;

			VoxelPalette palette = voxelEditor.VoxelPalette;
			if (palette == null) return;

			int allItemCount = palette.Length;

			int columns = 1;
			while (rect.width >= columns * minWidth + (columns - 1) * smallSpacing)
				columns++;

			float itemWidth = (rect.width - (columns - 1) * smallSpacing) / columns;
			int rows = Mathf.CeilToInt(allItemCount / (float)columns);
			float fullHeight = rows * height + (rows - 1) * smallSpacing;

			var r = new Rect(rect.x, rect.y, itemWidth, height);
			for (int i = 0; i < palette.Length; i++)
			{

				PaletteItem item = palette.GetItem(i);
				GUI.backgroundColor = item.color;
				if (GUI.Button(r, PaletteIndex == i ? "X" : ""))
					_editorComponent.SelectedPaletteIndex = i;

				r.x += itemWidth + smallSpacing;
				if (i % columns == 0)
				{
					r.x = rect.x;
					r.y += height + smallSpacing;
				}
			}

			GUI.backgroundColor = Color.white;
			GUI.enabled = _editorComponent.EnableEdit;
		}
	}
}
#endif