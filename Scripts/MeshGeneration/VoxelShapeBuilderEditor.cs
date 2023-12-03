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
		// SerializedProperty previewExtraSettingProperty;
		SerializedProperty quickVersionProperty;

		void OnEnable()
		{
			previewMaterialProperty = serializedObject.FindProperty(nameof(VoxelShapeBuilder.previewMaterial));
			// previewExtraSettingProperty = serializedObject.FindProperty(nameof(VoxelShapeBuilder.previewExtraSetting));
			quickVersionProperty = serializedObject.FindProperty(nameof(VoxelShapeBuilder.quickVersion));
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			VoxelShapeBuilder builder = (VoxelShapeBuilder)target;
			CustomMeshPreview preview = VoxelShapeBuilder.meshPreview;

			float fullWidth = EditorGUIUtility.currentViewWidth; 
			EditorGUILayout.Space();
			Rect messageRect = GUILayoutUtility.GetRect(fullWidth, 24);
			EasyMessageDrawer.DrawMessage(messageRect, 
				"Use another VoxelShapeBuilder for generating quickly or for Collider",
				EasyInspector.MessageType.Info, 12);
			EditorGUILayout.PropertyField(quickVersionProperty);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
			if (GUILayout.Button("Initialize Setup & Preview"))
			{
				builder.InitializeAndSetupPreview();
				preview.SetDirty();
			}

			EditorGUILayout.PropertyField(previewMaterialProperty); 
			serializedObject.ApplyModifiedProperties();

			// Extra Controls
			
			int voxelValue = 0;
			voxelValue.SetExtraVoxelData((ushort)builder.previewExtraSetting); 
			int lines = builder.GetExtraControls()?.Count ?? 0;
			Rect extraControlsRect = EditorGUILayout.GetControlRect(false, EditorHelper.GetStandardPanelHeight(lines));
			int newVoxelValue = DrawExtraControls(builder, voxelValue, builder, ref extraControlsRect);
			builder.previewExtraSetting = newVoxelValue.GetExtraVoxelData(); 

			// Preview

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
		}

		public static int DrawExtraControls(VoxelShapeBuilder selectedShape, int voxelData, Object recordedObj, ref Rect position)
		{
			IReadOnlyList<ExtraControl> extraControls = selectedShape == null ? null : selectedShape.GetExtraControls();
			if (extraControls == null || extraControls.Count == 0) return voxelData;

			int controlCount = extraControls.Count;

			Rect fullRect = position.SliceOut(EditorHelper.GetStandardPanelHeight(controlCount));

			Undo.RecordObject(recordedObj, "Selected Value Changed");
			ushort extraVoxelData = voxelData.GetExtraVoxelData();


			foreach (ExtraControl control in extraControls)
			{
				Rect controlRect = fullRect.SliceOutLine();
				EditorGUI.LabelField(controlRect, control.name);
				controlRect = EditorHelper.ContentRect(controlRect);

				object oldValue = control.GetExtraData(extraVoxelData);
				bool isExpanded = true;
				object newValue = EditorHelper.AnythingField(controlRect, control.DataType, oldValue, GUIContent.none, ref isExpanded);

				extraVoxelData = control.SetExtraData(extraVoxelData, newValue);
			}
			voxelData.SetExtraVoxelData(extraVoxelData);
			return voxelData;
		}
	}
}
#endif