#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace VoxelSystem
{
	[CustomEditor(typeof(VoxelObject))]
	public class VoxelObjectEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			var voxelObject = target as VoxelObject;
			if (voxelObject == null)
				return;

			TransformLock tLock = voxelObject.TransformLock;
			Transform transform = voxelObject.transform;
			VoxelMap voxelMap = voxelObject.Map;

			GUI.enabled = tLock.rotation && tLock.scale;
			const RecordType recordType = RecordType.Map | RecordType.Transform;
			if (GUILayout.Button("Apply Scale & Rotation"))
			{
				voxelObject.RecordForUndo("Rotation Applied to Map", recordType);

				voxelMap.ApplyScale(transform);
				voxelMap.ApplyRotation(transform);
			}

			GUI.enabled = true;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Connected Scriptable Objects", EditorStyles.boldLabel);
			 
			voxelObject.ConnectedMap = (VoxelMapScriptableObject)
				EditorGUILayout.ObjectField("Connected Map", voxelObject.ConnectedMap, typeof(VoxelMapScriptableObject),
					allowSceneObjects: false);

			voxelObject.SetPalette((VoxelPalette)
				EditorGUILayout.ObjectField("Connected Builder", voxelObject.VoxelPalette, typeof(VoxelPalette),
					allowSceneObjects: false));

			serializedObject.ApplyModifiedProperties();


			EditorGUILayout.Space();
			GUI.enabled = !voxelObject.HasConnectedMap();
			if (GUILayout.Button("Export Voxel Map"))
			{
				ExportVoxelMap(voxelObject.ArrayMap);
			}

			GUI.enabled = true;

			if (voxelObject.references.meshFilter != null && voxelObject.references.meshFilter.sharedMesh != null)
			{
				ArrayVoxelMap map = voxelObject.ArrayMap;
				if (map != null)
				{
					if (GUILayout.Button("Export Mesh"))
					{
						ExportMesh(voxelObject.references.meshFilter);
					}
				}
			}


			EditorGUILayout.Space();
			if (GUILayout.Button("Regenerate Meshes"))
			{
				Debug.LogWarning("TODO: MISSONG");
			}
		}

		static void ExportVoxelMap(ArrayVoxelMap map)
		{
			string path = EditorUtility.SaveFilePanelInProject("Save Voxel Map", "VoxelMap", "asset", "Save Voxel Map");
			if (path.Length != 0)
			{
				VoxelMapScriptableObject newMap = CreateInstance<VoxelMapScriptableObject>();
				newMap.map = new ArrayVoxelMap();
				newMap.map.SetupFrom(map);
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