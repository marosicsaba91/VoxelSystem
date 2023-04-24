#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace VoxelSystem
{
	[CustomEditor(typeof(VoxelMapScriptableObject))]
	public class VoxelMapScriptableObjectEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			VoxelMapScriptableObject t = (target as VoxelMapScriptableObject);
			DrawVoxelMapInspector(t);
		}

		public static void DrawVoxelMapInspector(VoxelMapScriptableObject t)
		{
			Vector3Int size = t.map.Size;
			GUILayout.Label("Size:   x:" + size.x + "   y:" + size.y + "   z:" + size.z);
		}
	}
}
#endif