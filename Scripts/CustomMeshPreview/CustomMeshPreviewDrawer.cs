#if UNITY_EDITOR
using EasyInspector;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	[CustomPropertyDrawer(typeof(CustomMeshPreview))]
	class CustomMeshPreviewDrawer : PropertyDrawer
	{
		static PreviewRenderUtility renderer;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			CustomMeshPreview preview = property.GetObjectOfProperty() as CustomMeshPreview;
			Object targetObject = property.serializedObject.targetObject;

			Undo.RecordObject(targetObject, "CustomMeshPreview Changed");

			AssemblyReloadEvents.beforeAssemblyReload += SetupMeshPreview;
			AssemblyReloadEvents.beforeAssemblyReload -= SetupMeshPreview;

			position.height = EditorGUIUtility.singleLineHeight;
			if (preview.isExpandable)
			{
				property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
				position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				if (!property.isExpanded) return;
			}

			position.height = preview.TextureSize.y;

			if (Event.current.type == EventType.Repaint)
				DrawMesh(preview, position);

			HandleMouseMovement(position, preview);
		}

		void SetupMeshPreview()
		{
			renderer?.Cleanup();
			renderer = null;
		}

		public static void DrawMesh(CustomMeshPreview preview, Rect position)
		{
			if (preview.Mesh == null) return;
			if (preview.Material == null) return;

			float fullWidth = position.width;
			position.width = preview.TextureSize.x ;
			position.x += (fullWidth - position.width) / 2;

			GUI.DrawTexture(position, preview.PreviewTexture);
		}

		static Vector2 mouseDownPos = Vector2.zero;

		public static bool HandleMouseMovement(Rect position, CustomMeshPreview preview)
		{
			EventType type = Event.current.type;

			if (type == EventType.MouseDown)
				mouseDownPos = Event.current.mousePosition;

			else if (type == EventType.MouseDrag && position.Contains(mouseDownPos))
			{
				float x = -Event.current.delta.x / position.width * 60;
				float y = Event.current.delta.y / position.height * 60;

				if (x != 0 || y != 0)
				{
					if (Event.current.shift)
					{
						preview.LightAngle += new Vector2(x, y) * 3;
						Log($"Directional Light Direction - Horizontal: {preview.LightAngle.x},   Vertical: {preview.LightAngle.y} ");
					}
					else
					{
						preview.CameraAngle += new Vector2(x, y) * 3;
						Log($"Camera Direction - Horizontal: {preview.CameraAngle.x},   Vertical: {preview.CameraAngle.y} ");
					}
					Event.current.Use();
					return true;
				}
			}
			else if (type == EventType.ScrollWheel && position.Contains(Event.current.mousePosition))
			{
				float change = Event.current.delta.y;
				if (Event.current.shift)
				{
					preview.FieldOfView += change / 3;
					preview.FieldOfView = Mathf.Clamp(preview.FieldOfView, 5, 160);
					Log("Field of View: " + preview.FieldOfView);
				}
				else
				{
					preview.Zoom += -change * 0.01f;
					preview.Zoom = Mathf.Clamp(preview.Zoom, 0.1f, 5);

					Log("Zoom: " + preview.Zoom);
				}
				Event.current.Use();
				return true;
			}


			return false;

			void Log(string s)
			{
				if (preview.areChangesLogged)
					Debug.Log(s);
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			CustomMeshPreview preview = property.GetObjectOfProperty() as CustomMeshPreview;

			if (!preview.isExpandable)
				return preview.TextureSize.y;

			if (!property.isExpanded)
				return base.GetPropertyHeight(property, label);

			return preview.TextureSize.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
	}
}
#endif