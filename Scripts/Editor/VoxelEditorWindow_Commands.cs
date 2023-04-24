#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	public partial class VoxelEditorWindow
	{

		void Separate()
		{
			RecordVoxelObjectForUndo(_targetVoxelObject, "Separate");

			var separatedGo = new GameObject(_targetGameObject.name + " Separated");
			separatedGo.transform.parent = _targetGameObject.transform;
			separatedGo.transform.localPosition = _selectionMin;
			separatedGo.transform.localRotation = Quaternion.identity;
			separatedGo.transform.localScale = Vector3.one;
			Undo.RegisterCreatedObjectUndo(separatedGo, "Create object");

			if (_targetVoxelObject.references.meshCollider != null)
				separatedGo.AddComponent<MeshCollider>();

			VoxelObject separatedVo = separatedGo.AddComponent<VoxelObject>();
			separatedVo.ConnectedBuilder = _targetVoxelObject.ConnectedBuilder;
			separatedVo.Map = new(_selectionSize);
			separatedVo.Map.ClearWhole();
			separatedVo.RegenerateMesh();

			// TODO: SET NEW MAP
			separatedVo.Map.CopyFromOtherMap(_targetVoxelObject.Map, _selectionMin, Vector3Int.zero, _selectionSize);

			_targetVoxelObject.Map.SetRange(_selectionMin, _selectionMax, VoxelMap.VoxelAreaAction.Clear, SelectedPaletteIndex);
			_targetVoxelObject.RegenerateMesh();

			Selection.activeGameObject = separatedGo;
			_targetGameObject = separatedGo;
			_targetVoxelObject = separatedVo;
			ChangeTarget();

			Tool = VoxelTool.Non;
			separatedVo.RegenerateMesh();
		}

		void CopyUp()
		{

			VoxelObject parent = _targetGameObject.transform.parent.GetComponentInParent<VoxelObject>();
			RecordVoxelObjectForUndo(parent, "CopyUp");

			_targetVoxelObject.ApplyScale();
			_targetVoxelObject.ApplyRotation();

			//Vector3Int parentMax = parent.Map.Size;
			Vector3 childPos = _targetGameObject.transform.localPosition;
			Vector3Int childMin = new(Mathf.RoundToInt(childPos.x), Mathf.RoundToInt(childPos.y), Mathf.RoundToInt(childPos.z));
			//Vector3 childMax = childMin + targetVoxelObject.Map.Size;
			/*
            if (childMin.x < 0 || childMin.y < 0 || childMin.x < 0 ||
                childMax.x > parentMax.x || childMax.y > parentMax.y || childMax.z > parentMax.z)
            {
                Debug.LogWarning("OUT OF BOUNDS" + "  " + childMin + "  " + childMax+"  " + parentMax);
                return;
            }
            */

			parent.Map.CopyFromOtherMap(_targetVoxelObject.Map, Vector3Int.zero, childMin, _targetVoxelObject.Map.Size);

			// targetGameObject.SetActive(false);

			/*
            Selection.activeGameObject = parent.gameObject;
            ChangeTarget();
            targetGameObject = parent.gameObject;
            targetVoxelObject = parent;

            tool = VoxelTool.NON;
            */

			parent.RegenerateMesh();
		}

	}
}
#endif