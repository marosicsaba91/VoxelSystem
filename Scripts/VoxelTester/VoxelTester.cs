using UnityEngine;
using VoxelSystem;
using System.Text;
using System.Collections.Generic;
using MUtility;
using EasyEditor;

public class VoxelTester : MonoBehaviour
{
	[SerializeField] bool enabledTest = true;

	[SerializeField] VoxelObject testedVoxelObject;
	[SerializeField] Vector3Int testedVoxelIndex;
	 
	[SerializeField] bool showVoxelInfo = true;
	[SerializeField] bool showOpenAndClosedSides = true;

	VoxelHit lastHitVoxel;
	VoxelObject lastHitObject;
	 
	public bool EnableTest => enabledTest;

	internal void Raycast(Ray ray, bool isClick)
	{
		VoxelObject tested = null;
		lastHitObject = null;
		lastHitVoxel = default;

		if (testedVoxelObject != null)
			tested = testedVoxelObject;
		else
		{
			if (Physics.Raycast(ray, out RaycastHit hit))
				if (hit.collider.TryGetComponent(out VoxelObject voxelObject))
					tested = voxelObject;
		}

		if (tested == null) return;
		if (!tested.Raycast(ray, out VoxelHit voxelHit)) return;

		lastHitObject = tested;
		lastHitVoxel = voxelHit;

		if (isClick)
		{
			testedVoxelObject = tested;
			testedVoxelIndex = voxelHit.voxelIndex;
		}
	}

	void OnDrawGizmosSelected()
	{
		if (lastHitObject != null)
		{
			Gizmos.matrix = lastHitObject.transform.localToWorldMatrix;
			Gizmos.color = selectionColor;
			DrawCube(lastHitVoxel.voxelIndex);
			EasyHandles.Matrix = Matrix4x4.identity;
			Gizmos.matrix = Matrix4x4.identity;
		}

		if (showOnlyOnSelected)
			Draw();
	}

	void OnDrawGizmos()
	{
		if (!showOnlyOnSelected)
			Draw();

	}

	void Draw()
	{
#if UNITY_EDITOR
		if (lastHitObject != null)
		{
			Gizmos.matrix = lastHitObject.transform.localToWorldMatrix;
			Gizmos.color = Color.magenta;
			DrawCube(lastHitVoxel.voxelIndex);

		}


		if (enabledTest && testedVoxelObject != null)
		{
			Gizmos.matrix = testedVoxelObject.transform.localToWorldMatrix;
			EasyHandles.Matrix = testedVoxelObject.transform.localToWorldMatrix;
			Gizmos.color = selectedColor;
			EasyHandles.Color = selectionColor;

			DrawCube(testedVoxelIndex);
			
			DrawVoxelInfo(testedVoxelObject, testedVoxelIndex);
			// DrawVoxelTransform(testedVoxelObject, testedVoxelIndex);
			DrawVoxelSides(testedVoxelObject, testedVoxelIndex);
		}
		EasyHandles.Matrix = Matrix4x4.identity;
		Gizmos.matrix = Matrix4x4.identity;
#endif
	}

	void DrawCube(Vector3Int index)
	{
		Vector3 center = index + Vector3.one * 0.5f;
		Gizmos.DrawWireCube(center, Vector3.one * 1.1f);
	}

	void DrawVoxelInfo(VoxelObject obj, Vector3Int index)
	{ 
		if (!showVoxelInfo) return;

		VoxelMap map = obj.GetVoxelMap();
		if (map == null) return;

		List<Material> materialPalette = null;
		VoxelShapePalette shapePalette = null;
		if (obj.TryGetComponent(out VoxelObject meshGenerator))
		{
			materialPalette = meshGenerator.MaterialPalette;
			shapePalette = meshGenerator.ShapePalette;
		}

		Vector3 center = index + Vector3.one * 0.5f;
		Vector3 position = center + Vector3.right * 1.2f;

		Voxel voxelValue = map.GetVoxel(index);


		StringBuilder text = new();
		text.AppendLine("GetCoordinate: " + index);
		text.AppendLine("Value: " + voxelValue);
		if (voxelValue.IsEmpty())
		{
			text.AppendLine("Empty");
		}
		else
		{
			int materialIndex = voxelValue.materialIndex;
			text.Append("Material: " + materialIndex);
			if (materialPalette != null && materialPalette.Count > materialIndex)
				text.Append(" (" + materialPalette[materialIndex].name + ")");
			text.AppendLine();
		}
		int shapeId = voxelValue.shapeId;
		text.Append("Shape: " + shapeId);
		if (shapePalette != null)
		{
			VoxelShapeBuilder shape = shapePalette.GetBuilder(shapeId);
			if(shape!= null)
				text.Append(" (" + shape.NiceName + ")");			
		}
		text.AppendLine();

		ushort extraVoxelData = voxelValue.extraVoxelData;
		text.AppendLine("ExtraVoxelData: " + extraVoxelData);

		EasyHandles.Color = textColor;
		EasyHandles.Label(position, text.ToString());
	}

	void DrawVoxelSides(VoxelObject obj, Vector3Int index)
	{
		if (!showOpenAndClosedSides) return;

		VoxelMap map = obj.GetVoxelMap();
		if (map == null) return;
		if (!obj.TryGetComponent(out VoxelObject meshGenerator)) return; 
		VoxelShapePalette shapePalette = meshGenerator.ShapePalette;
		if (shapePalette == null) return;

		Voxel voxelValue = map.GetVoxel(index); 
		int shapeIndex = voxelValue.shapeId;
		if (shapePalette.ItemCount <= shapeIndex) return;
	}

}
