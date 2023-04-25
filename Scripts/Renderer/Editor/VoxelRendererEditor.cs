using UnityEngine;
using UnityEditor;
using VoxelSystem;

[CustomEditor(typeof(VoxelRenderer))]
public class VoxelRendererEditor : Editor
{
	void OnSceneGUI()
	{
		VoxelRenderer renderer = (VoxelRenderer)target;

		// Subscribe to repaint event
		// SceneView.duringSceneGui -= Repaint;
		// SceneView.duringSceneGui += Repaint;

		Event e = Event.current;
		// if (e.type != EventType.MouseDown) return;

		// Get the position of the mouse click
		Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
		VoxelMap map = renderer.Map;
		if (map == null)
			return;

		// RAYCAST
		if (map.Raycast(ray, out InVoxelPoint hit, renderer.transform))
		{
			Debug.Log("Mesh clicked!");
		}
		else
		{
			Debug.Log("Missed");
		}
	}


	private void Repaint(SceneView sceneView)
	{
		if (Event.current.type != EventType.Repaint)
			return;

		VoxelRenderer renderer = (VoxelRenderer)target;
		if (renderer == null || !renderer.isActiveAndEnabled)
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

		Mesh mesh = renderer.Mesh;
		if (mesh == null)
			return;
		Material material = renderer.Material;
		if (material == null)
			return;

		Matrix4x4 matrix4X4 = renderer.LocalToWorldMatrix;
		Graphics.DrawMesh(mesh, matrix4X4, material, 0, sceneView.camera);
	}
}
