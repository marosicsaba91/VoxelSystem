using UnityEngine;
using UnityEditor;
using VoxelSystem;
using MUtility;
using UnityEngine.UI;

[CustomEditor(typeof(VoxelRenderer))]
public class VoxelRendererEditor : Editor
{
	//Static constructor
	static VoxelRendererEditor()
	{
		SceneView.duringSceneGui -= DuringSceneGUIEvent;
		SceneView.duringSceneGui += DuringSceneGUIEvent;
		Debug.Log("Static Constructor");
	}

	void OnSceneGUI()
	{
		VoxelRenderer renderer = (VoxelRenderer)target;

		// Subscribe to repaint event
		//SceneView.duringSceneGui -= DuringSceneGUIEvent;
		//SceneView.duringSceneGui += DuringSceneGUIEvent;

		// SceneView sceneView = SceneView.currentDrawingSceneView; 
	}


	static void DuringSceneGUIEvent(SceneView sceneView)
	{
		//if (Event.current.type != EventType.Repaint)
		//	return;
		/*
		EventType evetType = Event.current.type;
		VoxelRenderer[] voxelRenderers = FindObjectsByType<VoxelRenderer>(FindObjectsSortMode.None);
		foreach (VoxelRenderer renderer in voxelRenderers)
		{
			RenderCursor(renderer, sceneView.camera, evetType);
		}
		*/

	}

	static void RenderMesh(VoxelRenderer renderer, Camera camera)
	{
		Mesh mesh = renderer.Mesh;
		if (mesh == null)
			return;
		Material material = renderer.Material;
		if (material == null)
			return;

		Matrix4x4 matrix4X4 = renderer.LocalToWorldMatrix;
		Graphics.DrawMesh(mesh, matrix4X4, material, 0, camera);
	}

	public Ray ray;

	static void RenderCursor(VoxelRenderer renderer, Camera camera, EventType evetType)
	{ 
		OctVoxelMap map = renderer.Map;
		if (map == null) return;
		// RAYCAST

		Event e = Event.current;
		Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

		if (map.Raycast(ray, out VoxelHitPoint hit, renderer.transform))
		{
			Matrix4x4 transformMatrix = renderer.LocalToWorldMatrix;
			
			if (evetType is EventType.MouseDown or EventType.MouseUp or EventType.MouseDrag)
			{
				// selection.activeObject = renderer.gameObject;
				// Debug.Log(evetType);
			}
		}
	} 
}
