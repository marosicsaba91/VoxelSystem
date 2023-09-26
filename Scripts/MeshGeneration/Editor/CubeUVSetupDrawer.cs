#if UNITY_EDITOR
using MUtility;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	[CustomPropertyDrawer(typeof(CubeUVSetup))]

	public class CubeUVSetupDrawer : PropertyDrawer
	{
		const float rectSize = 250f; 

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			CubeUVSetup cubeUV = property.GetObjectOfProperty() as CubeUVSetup;
			EditorGUI.PropertyField(position, property, label, true);

			// Draw background
			if (!property.isExpanded) return;

			Rect fullRect = position;

			fullRect.x += (position.width - rectSize) / 2;
			fullRect.y = position.height - rectSize - 5f;
			fullRect.height = rectSize;
			fullRect.width = rectSize;
			EditorGUI.DrawRect(fullRect, Color.gray);

			//Draw grid

			int gridSize = cubeUV.gridSize;
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
			DrawSmall(fullRect, GeneralDirection3D.Back, cubeUV.back, Color.yellow, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Right, cubeUV.right, Color.red, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Left, cubeUV.left, Color.cyan, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Up, cubeUV.top, Color.green, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Down, cubeUV.bottom, Color.magenta, gridSize);
			DrawSmall(fullRect, GeneralDirection3D.Forward, cubeUV.front, Color.blue, gridSize);

			cubeUV.OnValidate();
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


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
			property.isExpanded ?
			EditorGUI.GetPropertyHeight(property, label, true) + rectSize + 10f :
			EditorGUI.GetPropertyHeight(property, label, true);
	}

}
#endif