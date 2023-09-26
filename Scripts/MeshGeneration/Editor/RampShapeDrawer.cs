#if UNITY_EDITOR
using MUtility;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	[CustomPropertyDrawer(typeof(Ramp9Shape))]

	public class RampShapeDrawer : PropertyDrawer
	{
		const int dataRows = 5;
		const float bigSpacing = 25;
		static readonly float rowHeight = EditorGUIUtility.singleLineHeight;
		static readonly float vSpacing = EditorGUIUtility.standardVerticalSpacing;
		static readonly float valuesHeight =
			dataRows * EditorGUIUtility.singleLineHeight +
			(dataRows - 1) * EditorGUIUtility.standardVerticalSpacing;
		static readonly float fullHeight =
			rowHeight + valuesHeight
			+ EditorGUIUtility.standardVerticalSpacing;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Ramp9Shape rampShape = property.GetObjectOfProperty() as Ramp9Shape;
			Object targetObject = property.serializedObject.targetObject;

			//Draw default property field
			position.height = rowHeight;
			property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
			if (!property.isExpanded) return;

			position.y += rowHeight + vSpacing;
			bool changed = DrawValues(rampShape, targetObject, ref position);

			if (changed)
			{
				rampShape.Validate();
				property.serializedObject.ApplyModifiedProperties();
				object unityObj = property.GetObjectWithProperty();

				if (unityObj is IRamShapeHolder holder)
					holder.OnRampUpdate(rampShape);
			}
		}

		bool DrawValues(Ramp9Shape rampShape, Object targetObject, ref Rect position)
		{
			bool changed = false;
			changed |= DrawValueRow(rampShape, targetObject, 1, ref position);
			changed |= DrawValueRow(rampShape, targetObject, 0, ref position);
			changed |= DrawValueRow(rampShape, targetObject, -1, ref position);

			return changed;
		}

		bool DrawValueRow(Ramp9Shape rampShape, Object targetObject, int y, ref Rect position)
		{
			bool changed = false;
			float fullWidth = position.width;
			float oneCellWidth = (fullWidth - 2 * bigSpacing) / 3;
			position.height = rowHeight;

			Rect p = position;
			p.width = oneCellWidth;
			changed |= DrawValue(rampShape, targetObject, new Vector2Int(-1, y), ref p);
			changed |= DrawValue(rampShape, targetObject, new Vector2Int(0, y), ref p);
			changed |= DrawValue(rampShape, targetObject, new Vector2Int(1, y), ref p);

			position.y += rowHeight + vSpacing;
			return changed;
		}

		bool DrawValue(Ramp9Shape rampShape, Object targetObject, Vector2Int coordinate, ref Rect position)
		{
			const float smallSpacing = 4f;
			const float checkBoxWidth = 20f;
			float oneCellWidth = position.width;
			float valueWidth = oneCellWidth - checkBoxWidth - smallSpacing;

			bool isNodeSet = rampShape.IsNodeSet(coordinate);
			float nodeHeight = rampShape.GetNodeHeight(coordinate);

			Rect p = position;
			p.width = valueWidth;
			GUI.enabled = isNodeSet;
			float newHeightValue = EditorGUI.Slider(p, nodeHeight, 0, 1);
			GUI.enabled = true;

			p.width = checkBoxWidth;
			p.x += valueWidth + smallSpacing;
			bool newIsSetValue = EditorGUI.Toggle(p, isNodeSet);

			bool changed = newHeightValue != nodeHeight || newIsSetValue != isNodeSet;
			if (changed)
				Undo.RecordObject(targetObject, "Change ramp shape");

			if (newHeightValue != nodeHeight)
				rampShape.SetNodeHeightNoValidate(coordinate, newHeightValue);
			if (newIsSetValue != isNodeSet)
				rampShape.SetNodeNoValidate(coordinate, newIsSetValue);

			position.x += oneCellWidth + bigSpacing;
			return changed;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
			property.isExpanded ? fullHeight : rowHeight;
	}

}
#endif