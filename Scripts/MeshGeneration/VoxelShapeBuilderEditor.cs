#if UNITY_EDITOR

using EasyInspector;
using MUtility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	[CustomEditor(typeof(VoxelShapeBuilder), true)]
	class VoxelShapeBuilderEditor : Editor
	{

		SerializedProperty previewMaterialProperty;
		SerializedProperty quickVersionProperty;

		void OnEnable()
		{
			previewMaterialProperty = serializedObject.FindProperty(nameof(VoxelShapeBuilder.previewMaterial)); 
			quickVersionProperty = serializedObject.FindProperty(nameof(VoxelShapeBuilder.quickVersion));
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			VoxelShapeBuilder builder = (VoxelShapeBuilder)target;

			float fullWidth = EditorGUIUtility.currentViewWidth; 
			EditorGUILayout.Space();
			Rect messageRect = GUILayoutUtility.GetRect(fullWidth, 24);
			EasyMessageDrawer.DrawMessage(messageRect,
				"Use another Shape for quick generation",
				EasyInspector.MessageType.Info, 12);
			EditorGUILayout.PropertyField(quickVersionProperty);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
			CustomMeshPreview preview = builder.meshPreview;
			if (GUILayout.Button("Initialize Cached Data"))
			{
				builder.InitializeMeshCacheAndSave();
				builder.RecalculatePreviewMesh();
				preview.Render();
			}

			EditorGUILayout.PropertyField(previewMaterialProperty); 
			serializedObject.ApplyModifiedProperties();

			// Preview

			// Extra Controls
			int lines = builder.GetExtraControls()?.Count ?? 0;
			Rect extraControlsRect = EditorGUILayout.GetControlRect(false, EditorHelper.GetStandardPanelHeight(lines));
			ushort extraControls = builder.previewExtraSetting;
			ushort newExtraControls = DrawExtraControls(builder, extraControls, builder, ref extraControlsRect);
			if (extraControls != newExtraControls) 
			{
				builder.previewExtraSetting = newExtraControls;
				builder.RecalculatePreviewMesh();
				preview.Render();
			}

			// Get GUI Event 
			Rect rect = GUILayoutUtility.GetRect(256, 256);
			CustomMeshPreviewDrawer.HandleMouseMovement(rect, preview);
			if (Event.current.type == EventType.Repaint)
			{ 
				preview.TextureSize = new Vector2(rect.width, rect.height);
				preview.BackgroundType = CameraClearFlags.Skybox;
				preview.Material = builder.PreviewMaterial;
				preview.meshGetter = builder.GetPreviewMesh;
				EditorGUI.DrawPreviewTexture(rect, preview.PreviewTexture);
			}
			if (GUILayout.Button("Regenerate Preview"))
			{
				builder.RecalculatePreviewMesh();
				preview.Render();
			}
		}

		public static ushort DrawExtraControls(VoxelShapeBuilder selectedShape, ushort extraVoxelData, Object recordedObj, ref Rect position)
		{
			IReadOnlyList<ExtraVoxelControl> extraControls = selectedShape == null ? null : selectedShape.GetExtraControls();
			if (extraControls == null || extraControls.Count == 0) return extraVoxelData;

			int controlCount = extraControls.Count;

			Rect fullRect = position.SliceOut(EditorHelper.GetStandardPanelHeight(controlCount));

			Undo.RecordObject(recordedObj, "Selected Value Changed"); 


			foreach (ExtraVoxelControl control in extraControls)
			{
				Rect controlRect = fullRect.SliceOutLine();
				EditorGUI.LabelField(controlRect, control.name);
				controlRect = EditorHelper.ContentRect(controlRect);

				object oldValue = control.GetExtraData(extraVoxelData);
				bool isExpanded = true;
				object newValue = EditorHelper.AnythingField(controlRect, control.DataType, oldValue, GUIContent.none, ref isExpanded);

				extraVoxelData = control.SetExtraData(extraVoxelData, newValue);
			} 
			return extraVoxelData;
		}
	}
}
#endif