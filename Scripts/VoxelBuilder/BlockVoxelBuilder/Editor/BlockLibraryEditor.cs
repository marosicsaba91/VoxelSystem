#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using VoxelSystem;

namespace VoxelSystemEditor
{
	[CustomEditor(typeof(BlockLibraryGenerator))]
	public class BlockLibraryEditor : UnityEditor.Editor
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

			BlockLibraryGenerator blockLibrary = (BlockLibraryGenerator)target;
			if (blockLibrary == null || !blockLibrary.isActiveAndEnabled)
			{
				SceneView.duringSceneGui -= Repaint;
				return;

			}

			// bool test if we are in prefab mode:
			bool isPrefab = SceneView.currentDrawingSceneView != null;

			// Debug.Log(SceneView.currentDrawingSceneView.camera.name);
			// Debug.Log(blockLibrary.gameObject.scene.name);

			if (!isPrefab)
			{

				SceneView.duringSceneGui -= Repaint;
				return;
			}

			Mesh mesh = blockLibrary.Mesh;
			if (mesh == null)
				return;
			Material material = blockLibrary.Material;
			if (material == null)
				return;

			Matrix4x4 matrix4X4 = blockLibrary.LocalToWorldMatrix;
			Graphics.DrawMesh(mesh, matrix4X4, material, 0, sceneView.camera);
		}
	}
}
#endif