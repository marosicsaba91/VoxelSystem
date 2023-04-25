using MUtility;
using UnityEngine;
using VoxelSystem;

[ExecuteAlways]
[RequireComponent(typeof(VoxelFilter))]
class OctMapVisualizer : MonoBehaviour
{
	[SerializeField] VoxelFilter voxelFilter; 
	[SerializeField] Mesh mesh;
	[SerializeField] Material[] materials;

	[SerializeField, Range(0, 0.5f)] float gap = 0.1f;

	Matrix4x4 matrix;

	void OnValidate()
	{
		voxelFilter = GetComponent<VoxelFilter>();
	}

	void Update()
	{
		if (voxelFilter == null)
			return;

		OctVoxelMap octMap = voxelFilter.GetOctMap();
		if (octMap == null)
			return;

		if (mesh == null)
			return;

		var offset = Matrix4x4.TRS(octMap.RealSize / 2 * Vector3.one , Quaternion.identity, Vector3.one);
		matrix = offset * transform.localToWorldMatrix;

		Gizmos.color = Color.yellow;

		Vector3 size = octMap.RealSize * Vector3Int.one;
		OctVoxelChunk root = octMap.RootChunk;
		int level = octMap.LevelCount;
		transform.localScale = Vector3.one;
		Draw(Vector3.zero, size, root, level);
	}


	void Draw(Vector3 position, Vector3 size, OctVoxelChunk node, int level)
	{

		if (node.IsHomogenous)
		{
			if (node.Value == -1)
				return;  // Empty node should not exist in the tree
			size -= gap * Vector3.one;
			var matrix = Matrix4x4.TRS(position, transform.rotation, size);
			Material material = materials[Mathf.Clamp(level, 0, materials.Length - 1)];
			Graphics.DrawMesh(mesh, this.matrix * matrix, material, 0);
		}
		else
		{
			for (int i = 0; i < 8; i++)
			{
				if (!node.TryGetInnerChunk(i, out OctVoxelChunk child))
					continue;

				var dir = (SubVoxel)i;
				Vector3 childSize = size * 0.5f;
				Vector3 offset = dir.ToVector().MultiplyAllAxis(childSize / 2f);

				Draw(position + offset, childSize, child, level - 1);
			}
		}
	}
}