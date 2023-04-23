using MUtility;
using UnityEngine;
using VoxelSystem;

[ExecuteAlways]
class OcTreeDrawer:MonoBehaviour
{
    [SerializeField] VoxelMapScriptableObject octmap;
    [SerializeField] Mesh mesh;
    [SerializeField] Material[] materials;

    [SerializeField, Range(0,0.5f)] float gap = 0.1f;

    Matrix4x4 matrix;

    void Update()
    {
        if (octmap == null) return;
        if (mesh == null) return;

        OctTree map = octmap.octMap;

        matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.yellow;

        Vector3 size = map.Size;

        Draw(Vector3.zero, size, map.rootChunk, map.levels);

    }

    void Draw(Vector3 position, Vector3 size, OctTreeNode node, int level)
    {
        if (node == null) return;

        if (!node.IsMixed)
        {
            if (node.value == -1) return;  // Empty node should not exist in the tree
            size -= gap * Vector3.one;
            Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, size);
            Material material = materials[Mathf.Clamp(level, 0, materials.Length - 1)];
            Graphics.DrawMesh(mesh, this.matrix * matrix, material, 0);

            //Gizmos.color = color;
            ///Gizmos.DrawCube(position, size);
        }
        else
        {
            // color.a *= 0.5f;
            for (int i = 0; i < node.innerChunks.Length; i++)
            {
                OctTreeNode child = node.innerChunks[i];
                if (child == null) continue;

                SubVoxel dir = (SubVoxel)i;
                Vector3 childSize = size * 0.5f;
                Vector3 offset = dir.ToVector().MultiplyAllAxis(childSize / 2f);

                Draw(position + offset, childSize, child, level - 1);
            }
        }
    }

}