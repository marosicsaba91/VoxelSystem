using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[CreateAssetMenu(fileName = "MeshVoxelShape", menuName = EditorConstants.categoryPath+ "VoxelShape: Mesh", order = EditorConstants.soOrder_VoxelShape)]
public class VoxelShape_Mesh : VoxelShapeBuilder
{
	[SerializeField] Mesh mesh; 
	[SerializeField] bool enableTransform = true;
	[SerializeField] bool autoConvertFromRightHanded = true;
	[SerializeField] SideFlags isSideFilled = new(false);

	public override bool IsTransformEnabled => enableTransform;

	static readonly Matrix4x4 rightToLeftHanded = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90, 0, 0), new Vector3(-1, -1, 1));
	readonly Dictionary<(Vector3Int, Flip3D), ArrayMesh> transformedMeshes = new();

	Vector3Int ToScaleVector(Flip3D flip) => flip switch
	{
		Flip3D.None => Vector3Int.one,
		Flip3D.X => new Vector3Int(-1, 1, 1),
		Flip3D.Y => new Vector3Int(1, -1, 1),
		Flip3D.Z => new Vector3Int(1, 1, -1),
		_ => throw new System.NotImplementedException()
	};

	static readonly Vector3 half = Vector3.one * 0.5f;

	protected sealed override void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder)
	{
		RegenerateMatrixDictionary();
		int vertexIndex = meshBuilder.VertexCount;

		for (int voxelIndex = 0; voxelIndex < voxelPositions.Count; voxelIndex++)
		{
			Vector3Int position = voxelPositions[voxelIndex];
			int vertexValue = map.GetVoxel(position);

			Flip3D flip = vertexValue.GetFlip();
			Vector3Int rotation = vertexValue.GetRotation();
			ArrayMesh transformedMesh = transformedMeshes[(rotation, flip)];

			int vertexCount = transformedMesh.vertices.Length;
			Vector3 center = position + half;

			for (int verticesIndex = 0; verticesIndex < vertexCount; verticesIndex++)
			{
				Vector3 local = transformedMesh.vertices[verticesIndex];
				Vector3 global = center + local;

				meshBuilder.AddVertex(global,transformedMesh.normals[verticesIndex],transformedMesh.uv[verticesIndex]);
			}

			for (int triangleIndex = 0; triangleIndex < transformedMesh.triangles.Length; triangleIndex++)
				meshBuilder.triangles.Add(transformedMesh.triangles[triangleIndex] + vertexIndex);

			vertexIndex += vertexCount;
		}
	}

	void RegenerateMatrixDictionary()
	{
		if (transformedMeshes.Count != 0) return;

		Matrix4x4 baseMatrix = autoConvertFromRightHanded ? rightToLeftHanded : Matrix4x4.identity;
		transformedMeshes.Clear();
		for (int x = 0; x <= 3; x++)
			for (int y = 0; y <= 3; y++)
				for (int z = 0; z <= 3; z++)
					for (int flipI = 0; flipI <= 3; flipI++)
					{
						Vector3Int scale = ToScaleVector((Flip3D)flipI);
						Vector3Int rotationI = new(x, y, z);
						Vector3 rotationE = rotationI * 90;
						Quaternion rotationQ = Quaternion.Euler(rotationE);
						Matrix4x4 transformation = Matrix4x4.TRS(Vector3.zero, rotationQ, scale) * baseMatrix;
						transformedMeshes.Add((rotationI, (Flip3D)flipI), ArrayMesh.CreateFromMesh(mesh, transformation));
					}
	}

	protected override bool IsSideFilled(GeneralDirection3D dir) => isSideFilled[dir];
}
