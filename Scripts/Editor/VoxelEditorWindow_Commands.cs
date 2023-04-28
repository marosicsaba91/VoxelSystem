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
			/*
			RecordVoxelObjectForUndo(_targetVoxelObject, "Separate");

			var separatedGo = new GameObject(_targetGameObject.name + " Separated");
			separatedGo.transform.parent = _targetGameObject.transform;
			separatedGo.transform.SetLocalPositionAndRotation(_selectionMin, Quaternion.identity);
			separatedGo.transform.localScale = Vector3.one;
			Undo.RegisterCreatedObjectUndo(separatedGo, "Create object");

			if (_targetVoxelObject.references.meshCollider != null)
				separatedGo.AddComponent<MeshCollider>();

			VoxelObject separatedVo = separatedGo.AddComponent<VoxelObject>();
			separatedVo.ConnectedBuilder = _targetVoxelObject.Builder;
			separatedVo.ArrayMap = new(_selectionSize);
			separatedVo.ArrayMap.ClearWhole();
			separatedVo.RegenerateMesh();

			// TODO: SET NEW MAP
			separatedVo.ArrayMap.CopyFromOtherMap(_targetVoxelObject.Map, _selectionMin, Vector3Int.zero, _selectionSize);

			_targetVoxelObject.Map.SetRange(_selectionMin, _selectionMax, VoxelMap.SetAction.Clear, SelectedPaletteIndex);
			_targetVoxelObject.RegenerateMesh();

			Selection.activeGameObject = separatedGo;
			_targetGameObject = separatedGo;
			_targetVoxelObject = separatedVo;
			ChangeTarget();

			Tool = VoxelTool.Non;
			separatedVo.RegenerateMesh();
		*/
		}

		void CopyUp()
		{

			IVoxelEditable parent = _targetGameObject.transform.parent.GetComponentInParent<IVoxelEditable>();
			RecordVoxelObjectForUndo(parent, "CopyUp");

			_targetVoxelObject.ApplyScale();
			_targetVoxelObject.ApplyRotation();

			Vector3 childPos = _targetGameObject.transform.localPosition;
			Vector3Int childMin = new(Mathf.RoundToInt(childPos.x), Mathf.RoundToInt(childPos.y), Mathf.RoundToInt(childPos.z));

			parent.Map.CopyFrom(_targetVoxelObject.Map, Vector3Int.zero, childMin, _targetVoxelObject.Map.FullSize);
			parent.RegenerateMesh();
		}

	}
}
#endif