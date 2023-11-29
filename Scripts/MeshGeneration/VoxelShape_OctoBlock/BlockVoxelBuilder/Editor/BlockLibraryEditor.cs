#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace VoxelSystem
{
	[CustomEditor(typeof(OctoBlockLibraryGenerator))]
	public class BlockLibraryEditor : Editor
	{
		public override void OnInspectorGUI() => base.OnInspectorGUI();

		void OnSceneGUI()
		{
			// Subscribe to repaint event
			SceneView.duringSceneGui -= Repaint;
			SceneView.duringSceneGui += Repaint;
		}

		void Repaint(SceneView sceneView)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			OctoBlockLibraryGenerator blockLibrary = (OctoBlockLibraryGenerator)target;
			if (blockLibrary == null || !blockLibrary.isActiveAndEnabled)
			{
				SceneView.duringSceneGui -= Repaint;
				return;
			}

			// bool test if we are in prefab mode:
			bool isPrefab = SceneView.currentDrawingSceneView != null;

			if (!isPrefab)
			{

				SceneView.duringSceneGui -= Repaint;
				return;
			}

			//Material material = blockLibrary.Material;
			//if (material == null)
			//	return;

			//Matrix4x4 matrix4X4 = blockLibrary.LocalToWorldMatrix;
			//Graphics.DrawMesh(previewMesh, matrix4X4, material, 0, sceneView.camera);
		}
	}
}
#endif