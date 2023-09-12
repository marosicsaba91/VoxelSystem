#if UNITY_EDITOR
using MUtility;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	// PropertyDrawer for CubeTextureCoordinates2
	[CustomPropertyDrawer(typeof(CubeTextureCoordinates2))]

	public class CubeTextureCoordinatesDrawer : PropertyDrawer
	{
		const float rectSize = 250f; 

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// SerializedProperty gridSize = property.FindPropertyRelative("gridSize");
			// SerializedProperty right = property.FindPropertyRelative("right");
			// SerializedProperty left = property.FindPropertyRelative("left");
			// SerializedProperty top = property.FindPropertyRelative("top");
			// SerializedProperty bottom = property.FindPropertyRelative("bottom");
			// SerializedProperty front = property.FindPropertyRelative("front");
			// SerializedProperty back = property.FindPropertyRelative("back");

			CubeTextureCoordinates2 coordinates = property.GetObjectOfProperty() as CubeTextureCoordinates2;

			//Draw default property field
			EditorGUI.PropertyField(position, property, label, true);

			// Draw background
			Rect fullRect = position;

			fullRect.x += (position.width - rectSize) / 2;
			fullRect.y = position.height - rectSize - 5f;
			fullRect.height = rectSize;
			fullRect.width = rectSize;
			EditorGUI.DrawRect(fullRect, Color.gray);

			//Draw grid

			int gridSize = coordinates.gridSize;
			float cellSize = rectSize / gridSize;
			for (int i = 0; i <= gridSize; i++)
			{
				float x = i * cellSize;
				Rect p = fullRect;
				p.x += x;
				p.width = 1;
				EditorGUI.DrawRect(p, Color.white);

				p.x -= x;
				p.y += x;
				p.width = rectSize;
				p.height = 1;
				EditorGUI.DrawRect(p, Color.white);
			}

			//Draw small rects
			DrawSmall(fullRect, GeneralDirection3D.Back, coordinates.back, Color.yellow, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Right, coordinates.right, Color.red, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Left, coordinates.left, Color.cyan, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Up, coordinates.top, Color.green, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Down, coordinates.bottom, Color.magenta, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Forward, coordinates.front, Color.blue, gridSize);
		}

		void DrawSmall(Rect fullRect, GeneralDirection3D dir, Vector2Int gridIndex, Color color, int gridCount)
		{
			float gridSize = rectSize / gridCount;
			float smallRectSize = gridSize * 0.25f;

			Rect p = fullRect;
			p.x -= smallRectSize / 2;
			p.x += (gridIndex.x + 0.5f) * gridSize;
			p.y += fullRect.height - smallRectSize / 2;
			p.y -= (gridIndex.y + 0.5f) * gridSize;

			Vector3 offset = dir.ToVector();
			offset.y = -offset.y;
			offset += 0.075f * offset.z * Vector3.one;


			p.position += 0.325f * gridSize * (Vector2)offset;

			p.width = smallRectSize;
			p.height = smallRectSize;
			EditorGUI.DrawRect(p, color);
		}


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property, label, true) + rectSize + 10f;
	}

}
#endif