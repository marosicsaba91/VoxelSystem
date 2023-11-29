using UnityEngine;
using VoxelSystem;
using System.Text;
using MUtility;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VoxelTester : MonoBehaviour
{
	[SerializeField] bool enabledTest = true;

	[SerializeField] VoxelObject testedVoxelObject;
	[SerializeField] Vector3Int testedVoxelIndex;
	 
	[SerializeField] bool showVoxelInfo = true;
	[SerializeField] bool showVoxelTransformation = true;
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
			Handles.matrix = testedVoxelObject.transform.localToWorldMatrix;
			Gizmos.color = Color.green;
			Handles.color = Color.magenta;

			DrawCube(testedVoxelIndex);
			
			DrawVoxelInfo(testedVoxelObject, testedVoxelIndex);
			DrawVoxelTransform(testedVoxelObject, testedVoxelIndex);
			DrawVoxelSides(testedVoxelObject, testedVoxelIndex);
		}

		Handles.matrix = Matrix4x4.identity;
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
#if UNITY_EDITOR
		if (!showVoxelInfo) return;

		VoxelMap map = obj.GetVoxelMap();
		if (map == null) return;

		MaterialPalette materialPalette = null;
		VoxelShapePalette shapePalette = null;
		if (obj.TryGetComponent(out VoxelMeshGenerator meshGenerator))
		{
			materialPalette = meshGenerator.MaterialPalette;
			shapePalette = meshGenerator.ShapePalette;
		}

		Vector3 center = index + Vector3.one * 0.5f;
		Vector3 position = center + Vector3.right * 1.2f;

		int voxelValue = map.GetVoxel(index);


		StringBuilder text = new();
		text.AppendLine("Index: " + index);
		text.AppendLine("Value: " + voxelValue);
		if (voxelValue.IsEmpty())
		{
			text.AppendLine("Empty");
		}
		else
		{
			int materialIndex = voxelValue.GetMaterialIndex();
			text.Append("Material: " + materialIndex);
			if (materialPalette != null && materialPalette.Count > materialIndex)
				text.Append(" (" + materialPalette[materialIndex].Material.name + ")");
			text.AppendLine();
		}
		int shapeIndex = voxelValue.GetShapeIndex();
		text.Append("Shape: " + shapeIndex);
		if (shapePalette != null && shapePalette.PaletteItems.Count > shapeIndex)
			text.Append(" (" + shapePalette.PaletteItems[shapeIndex].DisplayName + ")");
		text.AppendLine();

		ushort extraVoxelData = voxelValue.GetExtraVoxelData();
		text.AppendLine("Flip: " + extraVoxelData.GetFlip());
		text.AppendLine("Rotation: " + extraVoxelData.GetRotation());

		Handles.Label(position, text.ToString());
#endif
	}

	void DrawVoxelTransform(VoxelObject obj, Vector3Int index)
	{
		if (!showVoxelTransformation) return;

		VoxelMap map = obj.GetVoxelMap();
		if (map == null) return;

		int voxelValue = map.GetVoxel(index);
		ushort extraVoxelData = voxelValue.GetExtraVoxelData();
		Flip3D flip= extraVoxelData.GetFlip();
		Vector3Int rotation = extraVoxelData.GetRotation();

		Vector3 center = index + Vector3.one * 0.5f;

		Vector3 right = GeneralDirection3D.Right.Transform(flip, rotation).ToVector();
		Vector3 up = GeneralDirection3D.Up.Transform(flip, rotation).ToVector();
		Vector3 forward = GeneralDirection3D.Forward.Transform(flip, rotation).ToVector();

		Color originalColor = Gizmos.color;

		Gizmos.color = Color.red;
		Gizmos.DrawLine(center, center + right * 1.5f);
		Gizmos.color = Color.green;
		Gizmos.DrawLine(center, center + up * 1.5f);
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(center, center + forward * 1.5f);

		Gizmos.color = originalColor;
	}

	void DrawVoxelSides(VoxelObject obj, Vector3Int index)
	{
		if (!showOpenAndClosedSides) return;

		VoxelMap map = obj.GetVoxelMap();
		if (map == null) return;
		if (!obj.TryGetComponent(out VoxelMeshGenerator meshGenerator)) return; 
		VoxelShapePalette shapePalette = meshGenerator.ShapePalette;
		if (shapePalette == null) return;

		int voxelValue = map.GetVoxel(index); 
		int shapeIndex = voxelValue.GetShapeIndex();
		if (shapePalette.PaletteItems.Count <= shapeIndex) return;

		VoxelShapeBuilder shape = shapePalette.PaletteItems[shapeIndex] as VoxelShapeBuilder;

		Vector3 center = index + Vector3.one * 0.5f;
		// ushort extraVoxelData = voxelValue.GetExtraVoxelData();
		// Flip3D flip = extraVoxelData.GetFlip();
		// Vector3Int rotation = extraVoxelData.GetRotation();


		foreach (GeneralDirection3D dir in DirectionUtility.generalDirection3DValues)
		{
			bool filled = shape.IsSideFilled(dir);
			Gizmos.color = filled ? Color.black : Color.white;
			Vector3 position = center + dir.ToVector() * 0.5f;
			if(filled)
				Gizmos.DrawSphere(position, 0.1f);
			else
				Gizmos.DrawWireSphere(position, 0.1f);
		}
	}

}
