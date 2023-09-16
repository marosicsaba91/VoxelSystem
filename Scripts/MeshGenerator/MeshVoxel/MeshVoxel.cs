using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;

[CreateAssetMenu(menuName = "Voxel System/Mesh Voxel")]
public class MeshVoxel : VoxelShape
{
	[SerializeField] Mesh mesh;
	[SerializeField] bool enableRotation = true;
	[SerializeField] bool enableFlip = true;
	[SerializeField] bool autoConvertFromRightHanded = true;

	public override bool IsFlipEnabled => enableFlip;
	public override bool IsRotationEnabled => enableRotation;


	static readonly Matrix4x4 rightToLeftHanded = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90, 0, 0), new Vector3(-1, -1, 1));
	readonly Dictionary<(Vector3Int, Flip), CustomMesh> transformedMeshes = new();

	protected override void BeforeMeshGeneration(VoxelMap map, VoxelShapePalette palette, int shapeIndex)
	{
		if (mesh == null) return;

		Matrix4x4 baseMatrix = autoConvertFromRightHanded ? rightToLeftHanded : Matrix4x4.identity;

		transformedMeshes.Clear();
		for (int x = 0; x <= 3; x++)
			for (int y = 0; y <= 3; y++)
				for (int z = 0; z <= 3; z++)
					for (int flipI = 0; flipI <= 3; flipI++)
					{
						Vector3Int scale = ToScaleVector((Flip)flipI);
						Vector3Int rotationI = new(x, y, z);
						Vector3 rotationE = rotationI * 90;
						Quaternion rotationQ = Quaternion.Euler(rotationE);
						Matrix4x4 transformation = Matrix4x4.TRS(Vector3.zero, rotationQ, scale) * baseMatrix;
						transformedMeshes.Add((rotationI, (Flip)flipI), CustomMesh.CreateFromMesh(mesh, transformation));
					}
	}

	Vector3Int ToScaleVector(Flip flip) => flip switch
	{
		Flip.None => Vector3Int.one,
		Flip.X => new Vector3Int(-1, 1, 1),
		Flip.Y => new Vector3Int(1, -1, 1),
		Flip.Z => new Vector3Int(1, 1, -1),
		_ => throw new System.NotImplementedException()
	};

	static readonly Vector3 half = Vector3.one * 0.5f;

	protected override void GenerateMeshData
		(VoxelMap map,
		List<Vector3Int> voxelPositions,
		int shapeIndex,
		List<Vector3> vertexOut,
		List<Vector3> normalOut,
		List<Vector2> uvOut,
		List<int> triangleOut)
	{
		if (mesh == null) return;

		int vertexIndex = vertexOut.Count;

		for (int voxelIndex = 0; voxelIndex < voxelPositions.Count; voxelIndex++)
		{
			Vector3Int position = voxelPositions[voxelIndex];
			int vertexValue = map.GetVoxel(position);

			Flip flip = vertexValue.GetFlip();
			Vector3Int rotation = vertexValue.GetRotation();
			CustomMesh transformedMesh = transformedMeshes[(rotation, flip)];

			int vertexCount = transformedMesh.vertices.Length;
			Vector3 center = position + half;

			for (int verticesIndex = 0; verticesIndex < vertexCount; verticesIndex++)
			{
				Vector3 local = transformedMesh.vertices[verticesIndex];
				Vector3 global = center + local;

				vertexOut.Add(global);


				normalOut.Add(transformedMesh.normals[verticesIndex]);
				uvOut.Add(transformedMesh.uv[verticesIndex]);
			}

			for (int triangleIndex = 0; triangleIndex < transformedMesh.triangles.Length; triangleIndex++)
			{
				triangleOut.Add(transformedMesh.triangles[triangleIndex] + vertexIndex);
			}

			vertexIndex += vertexCount;

		}
	}
}