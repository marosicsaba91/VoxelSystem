#if UNITY_EDITOR
using MUtility;
using UnityEngine;
using UnityEditor;

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

			if (_targetVoxelObject == null)
			{
				GUI.Label(position, "No VoxelObject Selected!\nSelect a VoxelObject to edit!", _boldMiddleTextStyle);
				return;
			}

			FindRects(out Rect mapRect, out Rect transformRect, out Rect editRect, out Rect paletteRect);

			/*
            // Debug Drawing the Main Areas
            EditorHelper.DrawBox(mapRect, new Color(0f, 1f, 1f, 0.3f)); 
            EditorHelper.DrawBox(transformRect,new Color(1f, 0, 1f, 0.3f)); 
            EditorHelper.DrawBox(editRect,new Color(1f, 1f, 0f, 0.3f));
            EditorHelper.DrawBox(paletteRect,new Color(0f, 1f, 0f, 0.3f));
            */

			DrawMapTools(mapRect);

			if (_targetVoxelObject.ConnectedBuilder == null)
				return;

			DrawTransform(transformRect);
			DrawEditTools(editRect);
			DrawPalette(paletteRect, _targetVoxelObject);
		}

		void SetupStyles()
		{
			// if (_normalButtonStyle == null)
			//return;

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

			string label = "Map";
			if (_targetVoxelObject.HasConnectedMap())
			{
				label += ": " + _targetVoxelObject.ConnectedMap.name;
			}

			GUI.Label(rect, label, _boldMiddleTextStyle);
			Vector3Int s = _targetVoxelObject.Map.Size;
			GUIStyle small = new(EditorStyles.centeredGreyMiniLabel);
			rect.y += lineHeight + smallSpacing;
			GUI.Label(rect, " (" + s.x + "x" + s.y + "x" + s.z + ")", small);

			if (_targetVoxelObject.ConnectedBuilder == null)
			{
				EditorGUILayout.Space();
				GUILayout.Label("VoxelObject Has No Builder!", _boldMiddleTextStyle);
				ClearEditor();
				return;
			}

			rect.y += lineHeight + smallSpacing;
			DrawToolLine(rect, new[] { VoxelTool.Select });
			rect.y += lineHeight + smallSpacing;
			DrawCommandLine(rect, new[] { VoxelAction.Clear, VoxelAction.Fill });
			rect.y += lineHeight + smallSpacing;
			DrawCommandLine(rect, new[] { VoxelAction.Separate, VoxelAction.CopyUp });
		}

		void DrawTransform(Rect rect)
		{
			const int lineCount = 5;
			float lineHeight = (rect.height - (lineCount - 1) * smallSpacing) / lineCount;
			rect.height = lineHeight;
			GUI.Label(rect, "Transform", _boldMiddleTextStyle);
			rect.y += lineHeight + smallSpacing;
			DrawToolLine(rect, _transformTools);
			rect.y += lineHeight + smallSpacing;
			DrawToolLine(rect, _sizeTools);
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
			ToggleButton(rect, "Position", ref _targetVoxelObject.lockPosition);
			rect.x += buttonWidth + smallSpacing;
			ToggleButton(rect, "Rotation", ref _targetVoxelObject.lockRotation);
			rect.x += buttonWidth + smallSpacing;
			ToggleButton(rect, "Scale", ref _targetVoxelObject.lockScale);

			void ToggleButton(Rect r, string label, ref bool value)
			{
				int textLength = _normalFont.GetTextLength(label);
				if (textLength > r.width - 12)
					label = label.Substring(0, 3);
				textLength = _normalFont.GetTextLength(label);
				if (textLength > r.width - 10)
					label = label.Substring(0, 1);

				GUI.backgroundColor = value ? Color.gray : Color.white;
				if (GUI.Button(rect, label))
					value = !value;
				GUI.backgroundColor = Color.white;
			}
		}

		void DrawTransformApplyButtons(Rect rect)
		{
			GUI.enabled = _targetVoxelObject.lockRotation && _targetVoxelObject.lockScale;
			if (GUI.Button(rect, "Apply Scale & Rotation"))
			{
				RecordVoxelObjectForUndo(_targetVoxelObject, "Rotation Applied to Map");
				_targetVoxelObject.ApplyScale();
				_targetVoxelObject.ApplyRotation();
			}

			GUI.enabled = true;
		}

		void DrawEditTools(Rect rect)
		{
			const int lineCount = 5;
			float lineHeight = (rect.height - (lineCount - 1) * smallSpacing) / lineCount;
			rect.height = lineHeight;
			GUI.Label(rect, "Edit", _boldMiddleTextStyle);
			rect.y += lineHeight + smallSpacing;
			rect.height += lineHeight + smallSpacing;
			DrawToolLine(rect, _basicEditTools);
			rect.y += rect.height + smallSpacing;
			DrawToolLine(rect, _secondaryTools);
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

		void DrawToolButton(Rect rect, VoxelTool buttonTool)
		{
			GUI.backgroundColor = buttonTool == Tool ? Color.gray : Color.white;

			if (GUI.Button(rect, buttonTool.ToString()))
			{
				if (Tool != buttonTool)
				{
					Tools.hidden = true;
					Tool = buttonTool;
				}
				else
				{
					Tools.hidden = false;
					Tool = VoxelTool.Non;
				}
			}

			GUI.backgroundColor = Color.white;
		}


		void DrawCommandLine(Rect rect, VoxelAction[] actions)
		{

			if (actions == null || actions.Length == 0)
				return;

			float buttonWidth = (rect.width - (actions.Length - 1) * smallSpacing) / actions.Length;
			rect.width = buttonWidth;
			foreach (VoxelAction t in actions)
			{
				GUI.enabled = IsActionEnabled(t);
				if (GUI.Button(rect, t.ToString()))
				{
					DoVoxelAction(t);
				}
				rect.x += buttonWidth + smallSpacing;
			}

			GUI.enabled = true;
		}

		bool IsActionEnabled(object action)
		{
			switch (action)
			{
				case VoxelAction.Fill:
				case VoxelAction.Clear:
					return true;
				case VoxelAction.CopyUp:
					return _targetGameObject.transform.parent != null &&
						   _targetGameObject.transform.parent.GetComponentInParent<VoxelObject>() != null &&
						   _targetVoxelObject.lockPosition &&
						   _targetVoxelObject.lockRotation &&
						   _targetVoxelObject.lockScale;
				case VoxelAction.Separate:
					return Tool == VoxelTool.Select;
			}

			return false;
		}

		void DoVoxelAction(VoxelAction action)
		{
			switch (action)
			{
				case VoxelAction.Clear:
					if (Tool != VoxelTool.Select)
					{
						RecordVoxelObjectForUndo(_targetVoxelObject, "Map Cleared");
						_targetVoxelObject.ClearWholeMap();
					}
					else
					{
						RecordVoxelObjectForUndo(_targetVoxelObject, "Selection Cleared");
						_targetVoxelObject.Map.SetRange(_selectionMin, _selectionMax, VoxelMap.SetAction.Clear,
							SelectedPaletteIndex);
					}

					break;
				case VoxelAction.Fill:
					if (Tool != VoxelTool.Select)
					{
						RecordVoxelObjectForUndo(_targetVoxelObject, "Map Filled");
						_targetVoxelObject.FillWholeMap(SelectedPaletteIndex);
					}
					else
					{
						RecordVoxelObjectForUndo(_targetVoxelObject, "Selection Filled");
						_targetVoxelObject.Map.SetRange(_selectionMin, _selectionMax, VoxelMap.SetAction.Fill,
							SelectedPaletteIndex);
					}

					break;
				case VoxelAction.CopyUp:
					CopyUp();
					break;
				case VoxelAction.Separate:
					Separate();
					break;
			}
		}

		static void ClickTotTool(VoxelTool clickedTool)
		{
			if (Tool != clickedTool)
			{
				Tools.hidden = true;
				Tool = clickedTool;
			}
			else
			{
				Tools.hidden = false;
				Tool = VoxelTool.Non;
			}
		}

		void DrawPalette(Rect rect, VoxelObject vo)
		{
			GUI.enabled = _paletteUsingTools.Contains(Tool);

			const float minWidth = 50;
			const float height = 40;

			int allItemCount = vo.ConnectedBuilder.PaletteLength;

			int columns = 1;
			while (rect.width >= columns * minWidth + (columns - 1) * smallSpacing)
				columns++;

			float itemWidth = (rect.width - (columns - 1) * smallSpacing) / columns;
			int rows = Mathf.CeilToInt(allItemCount / (float)columns);
			float fullHeight = rows * height + (rows - 1) * smallSpacing;

			var r = new Rect(rect.x, rect.y, itemWidth, height);
			int index = 0;
			foreach (PaletteItem item in vo.ConnectedBuilder.GetPaletteItems())
			{
				GUI.backgroundColor = item.color;
				if (GUI.Button(r, SelectedPaletteIndex == item.value ? "X" : ""))
				{
					SelectedPaletteIndex = item.value;
				}
				r.x += itemWidth + smallSpacing;
				index++;
				if (index % columns == 0)
				{
					r.x = rect.x;
					r.y += height + smallSpacing;
				}
			}

			GUI.backgroundColor = Color.white;
			GUI.enabled = true;
		}
	}
}
#endif