using UnityEngine;
using UnityEditor;
using VoxelSystem;
using MUtility;

[CustomEditor(typeof(VoxelRenderer))]
public class VoxelRendererEditor : Editor
{
	void OnSceneGUI()
	{
		VoxelRenderer renderer = (VoxelRenderer)target;

		// Subscribe to repaint event
		SceneView.duringSceneGui -= DuringSceneGUIEvent;
		SceneView.duringSceneGui += DuringSceneGUIEvent;

		// SceneView sceneView = SceneView.currentDrawingSceneView;

	}


	void DuringSceneGUIEvent(SceneView sceneView)
	{
		if (Event.current.type != EventType.Repaint)
			return;

		VoxelRenderer renderer = (VoxelRenderer)target;
		if (renderer == null || !renderer.isActiveAndEnabled)
		{
			SceneView.duringSceneGui -= DuringSceneGUIEvent;
			return;
		}

		// bool test if we are in prefab mode:
		/*
		bool isPrefab = SceneView.currentDrawingSceneView != null;

		if (!isPrefab)
		{
			SceneView.duringSceneGui -= DuringSceneGUIEvent;
			return;
		}
		*/

		// RenderMesh(sceneView, renderer);
		RenderCursor(renderer);
	}

	static void RenderMesh(SceneView sceneView, VoxelRenderer renderer)
	{
		Mesh mesh = renderer.Mesh;
		if (mesh == null)
			return;
		Material material = renderer.Material;
		if (material == null)
			return;

		Matrix4x4 matrix4X4 = renderer.LocalToWorldMatrix;
		Graphics.DrawMesh(mesh, matrix4X4, material, 0, sceneView.camera);
	}

	public Ray ray;

	void RenderCursor(VoxelRenderer renderer)
	{
		if (renderer.cursorMaterial == null) return;
		if (renderer.cursorMesh == null) return;
		OctVoxelMap map = renderer.Map;
		if (map == null) return;
		// RAYCAST

		Event e = Event.current;
		Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
		 
		if (map.Raycast(ray, out InVoxelPoint hit, renderer.transform))
		{ 
			Matrix4x4 matrix4X4 = renderer.LocalToWorldMatrix;
			matrix4X4 *= Matrix4x4.Translate(hit.point);
			Graphics.DrawMesh(renderer.cursorMesh, matrix4X4, renderer.cursorMaterial, 0);
		}
	}
}
