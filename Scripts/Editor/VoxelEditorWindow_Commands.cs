#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	public partial class VoxelEditorWindow
	{
		// TODO: RECOVER THIS

		// DON'T DELETE
		// DON'T DELETE
		// DON'T DELETE
		// DON'T DELETE
		// DON'T DELETE
		// DON'T DELETE
		
		void Separate()		
		{
			RecordVoxelObjectForUndo(_editorComponent, "Separate");
			var voxelObject = _editorComponent as VoxelObject;
			if (voxelObject == null) 
			{
				Debug.LogError("Separate is only available for VoxelObject");
				return;
			}

			var separatedGo = new GameObject(_targetGameObject.name + " Separated");
			separatedGo.transform.parent = _targetGameObject.transform;
			separatedGo.transform.SetLocalPositionAndRotation(_selectionMin, Quaternion.identity);
			separatedGo.transform.localScale = Vector3.one;
			Undo.RegisterCreatedObjectUndo(separatedGo, "Create object");

			if (voxelObject.references.meshCollider != null)
				separatedGo.AddComponent<MeshCollider>();

			VoxelObject separatedVo = separatedGo.AddComponent<VoxelObject>();
			separatedVo.ConnectedBuilder = voxelObject.ConnectedBuilder;
			separatedVo.ArrayMap = new(_selectionSize);
			separatedVo.ArrayMap.ClearWhole();

			// TODO: SET NEW MAP
			separatedVo.ArrayMap.CopyFrom(_editorComponent.Map, _selectionMin, Vector3Int.zero, _selectionSize);

			_editorComponent.Map.SetRange(_selectionMin, _selectionMax, VoxelAction.Erase, SelectedPaletteIndex);

			Selection.activeGameObject = separatedGo;
			_targetGameObject = separatedGo;
			_editorComponent = separatedVo;
			ChangeTarget();

			SelectedTool = VoxelTool.None;
		
		}

		void CopyUp()
		{
			var voxelObject = _editorComponent as VoxelObject;
			if (voxelObject == null)
			{
				Debug.LogError("CopyUp is only available for VoxelObject");
				return;
			}

			IVoxelEditable parent = _targetGameObject.transform.parent.GetComponentInParent<IVoxelEditable>();
			RecordVoxelObjectForUndo(parent, "CopyUp");

			VoxelMap map = _editorComponent.Map;
			Transform transform = _targetGameObject.transform;

			map.ApplyScale(transform);
			map.ApplyRotation(transform);

			Vector3 childPos = _targetGameObject.transform.localPosition;
			Vector3Int childMin = new(Mathf.RoundToInt(childPos.x), Mathf.RoundToInt(childPos.y), Mathf.RoundToInt(childPos.z));

			parent.Map.CopyFrom(map, Vector3Int.zero, childMin, map.FullSize);
			voxelObject.RegenerateMesh();
		}
	}
}
#endif