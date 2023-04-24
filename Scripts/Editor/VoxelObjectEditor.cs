#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace VoxelSystem
{
	[CustomEditor(typeof(VoxelObject))]
	public class VoxelObjectEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			var t = target as VoxelObject;
			if (t == null)
				return;

			GUI.enabled = t.lockRotation && t.lockScale;
			if (GUILayout.Button("Apply Scale & Rotation"))
			{
				VoxelEditorWindow.RecordVoxelObjectForUndo(t, "Rotation Applied to Map");
				t.ApplyScale();
				t.ApplyRotation();
			}

			GUI.enabled = true;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Connected Scriptable Objects", EditorStyles.boldLabel);

			Undo.RecordObject(t, "Connected Map Changed");
			t.ConnectedMap = (VoxelMapScriptableObject)
				EditorGUILayout.ObjectField("Connected Map", t.ConnectedMap, typeof(VoxelMapScriptableObject),
					allowSceneObjects: false);

			Undo.RecordObject(t, "Connected Builder Changed");
			t.ConnectedBuilder = (VoxelBuilder)
				EditorGUILayout.ObjectField("Connected Builder", t.ConnectedBuilder, typeof(VoxelBuilder),
					allowSceneObjects: false);

			serializedObject.ApplyModifiedProperties();


			EditorGUILayout.Space();
			GUI.enabled = !t.HasConnectedMap();
			if (GUILayout.Button("Export Voxel Map"))
			{
				ExportVoxelMap(t.Map);
			}

			GUI.enabled = true;

			if (t.references.meshFilter != null && t.references.meshFilter.sharedMesh != null)
			{
				VoxelMap map = t.Map;
				if (map != null)
				{
					if (GUILayout.Button("Export Mesh"))
					{
						ExportMesh(t.references.meshFilter);
					}
				}
			}


			EditorGUILayout.Space();
			if (GUILayout.Button("Regenerate Meshes"))
			{
				Undo.RecordObjects(new Object[] { t.references.meshRenderer, t.references.meshFilter },
					"Connected Map Changed");
				t.RegenerateMesh();
			}
		}

		static void ExportVoxelMap(VoxelMap map)
		{
			string path = EditorUtility.SaveFilePanelInProject("Save Voxel Map", "VoxelMap", "asset", "Save Voxel Map");
			if (path.Length != 0)
			{
				VoxelMapScriptableObject newMap = CreateInstance<VoxelMapScriptableObject>();
				newMap.map = map.GetCopy();
				newMap.name = Path.GetFileName(path);
				AssetDatabase.CreateAsset(newMap, path);
				AssetDatabase.Refresh();
			}
		}

		public class HandleExample : MonoBehaviour
		{
			public float shieldArea = 5.0f;
		}

		protected void DrawHandles()
		{
			HandleExample handleExample = new();

			if (handleExample == null)
			{
				return;
			}

			Handles.color = Color.yellow;

			GUIStyle style = new();
			style.normal.textColor = Color.green;

			Vector3 position = handleExample.transform.position + Vector3.up * 2f;
			string posString = position.ToString();

			Handles.Label(position,
				posString + "\nShieldArea: " +
				handleExample.shieldArea,
				style
			);

			Handles.BeginGUI();
			if (GUILayout.Button("Reset Area", GUILayout.Width(width: 100)))
			{
				handleExample.shieldArea = 5;
			}
			Handles.EndGUI();

			Handles.DrawWireArc(
				handleExample.transform.position,
				handleExample.transform.up,
				-handleExample.transform.right,
				angle: 180,
				handleExample.shieldArea);

			handleExample.shieldArea =
				Handles.ScaleValueHandle(handleExample.shieldArea,
					handleExample.transform.position + handleExample.transform.forward * handleExample.shieldArea,
					handleExample.transform.rotation,
					size: 1, Handles.ConeHandleCap, snap: 1);
		}

		void ExportMesh(MeshFilter meshFilter)
		{
			string path = EditorUtility.SaveFilePanelInProject("Export Mesh file", "Mesh", "obj", "Export Mesh file");
			if (path.Length != 0)
			{
				MeshToObjExporter.MeshToFile(meshFilter, path);
				AssetDatabase.Refresh();
			}
		}
	}
}
#endif