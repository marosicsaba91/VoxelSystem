#if UNITY_EDITOR

using EasyEditor;
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
				EasyEditor.MessageType.Info, 12);
			EditorGUILayout.PropertyField(quickVersionProperty);
			EditorGUILayout.Space();
			CustomMeshPreview preview = builder.meshPreview;
			if (GUILayout.Button("Initialize Cached Data"))
			{
				builder.InitializeMeshCacheAndSave();
				builder.RecalculatePreviewMesh();
				preview.Render();
			}

			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(previewMaterialProperty);
			serializedObject.ApplyModifiedProperties();

			// Preview

			// Extra Controls
			CubicTransformation transformation = builder.previewTransformation;
			CubicTransformation newTransform = transformation;
			byte extraControls = builder.previewExtraSetting;

			int lines = builder.GetExtraControls()?.Count ?? 0;
			Rect extraControlsRect = EditorGUILayout.GetControlRect(false, EditorHelper.GetStandardPanelHeight(lines));
			byte newExtraData = DrawExtraControls(builder, extraControls, ref extraControlsRect);



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

			if (builder.SupportsTransformation)
			{
				Rect cubicTransformRect = EditorGUILayout.GetControlRect(false, EditorHelper.GetStandardPanelHeight(1));
				newTransform = DrawCubicTransformation(transformation, ref cubicTransformRect);
			}

			if (GUILayout.Button("Regenerate Preview"))
			{
				builder.RecalculatePreviewMesh();
				preview.Render();
			}

			if (extraControls != newExtraData || transformation != newTransform)
			{
				Undo.RecordObject(builder, "Selected Value Changed");
				builder.previewTransformation = newTransform;
				builder.previewExtraSetting = newExtraData;
				builder.RecalculatePreviewMesh();
				preview.Render();
			}
		}

		public static CubicTransformation DrawCubicTransformation(CubicTransformation transformation, ref Rect position)
		{
			CubicTransformation newTransform = transformation;
			Rect full = position.SliceOutLine();
			position.RemoveOneSpace();

			Rect r = full.SliceOut(100, Side.Left);
			GUIContent gUIContent = new("Transformation", null, "Up Direction   /   Vertical Mirroring   /   Vertical Rotation");
			EditorGUI.LabelField(r, gUIContent);
			r = full.SliceOut(80, Side.Left); 
			newTransform.upDirection = (GeneralDirection3D)EditorGUI.EnumPopup(r, transformation.upDirection);
			r = full.SliceOut(18, Side.Left); 
			newTransform.isVerticalFlipped = EditorGUI.Toggle(r, transformation.isVerticalFlipped); 
			newTransform.verticalRotation = EditorGUI.IntSlider(full, transformation.verticalRotation, 0, 3);

			return newTransform;
		}

		public static byte DrawExtraControls(VoxelShapeBuilder selectedShape, byte extraVoxelData, ref Rect position)
		{
			IReadOnlyList<ExtraVoxelControl> extraControls = selectedShape == null ? null : selectedShape.GetExtraControls();
			if (extraControls == null || extraControls.Count == 0) return extraVoxelData;
			int controlCount = extraControls.Count;

			Rect fullRect = position.SliceOut(EditorHelper.GetStandardPanelHeight(controlCount));

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