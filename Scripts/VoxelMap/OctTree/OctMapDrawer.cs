using MUtility;
using UnityEngine;
using VoxelSystem;

[ExecuteAlways]
class OctMapDrawer : MonoBehaviour
{
	[SerializeField] VoxelMapScriptableObject octmap;
	[SerializeField] Mesh mesh;
	[SerializeField] Material[] materials;

	[SerializeField, Range(0, 0.5f)] float gap = 0.1f;


	Matrix4x4 matrix;

	void Update()
	{
		if (octmap == null)
			return;
		if (mesh == null)
			return;

		matrix = transform.localToWorldMatrix;

		Gizmos.color = Color.yellow;

		Vector3 size = octmap.octMap.RealSize * Vector3Int.one;
		OctVoxelChunk root = octmap.octMap.RootChunk;
		int level = octmap.octMap.LevelCount;
		Draw(Vector3.zero, size, root, level);
	}


	void Draw(Vector3 position, Vector3 size, OctVoxelChunk node, int level)
	{
		if (node == null)
			return;

		if (node.IsHomogenous)
		{
			if (node.Value == -1)
				return;  // Empty node should not exist in the tree
			size -= gap * Vector3.one;
			var matrix = Matrix4x4.TRS(position, Quaternion.identity, size);
			Material material = materials[Mathf.Clamp(level, 0, materials.Length - 1)];
			Graphics.DrawMesh(mesh, this.matrix * matrix, material, 0);
		}
		else
		{
			for (int i = 0; i < 8; i++)
			{
				OctVoxelChunk child = node.TryGetInnerNode(i);
				if (child == null)
					continue;

				var dir = (SubVoxel)i;
				Vector3 childSize = size * 0.5f;
				Vector3 offset = dir.ToVector().MultiplyAllAxis(childSize / 2f);

				Draw(position + offset, childSize, child, level - 1);
			}
		}
	}
}