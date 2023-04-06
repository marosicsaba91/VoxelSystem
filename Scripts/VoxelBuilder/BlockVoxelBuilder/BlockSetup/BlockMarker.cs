using UnityEngine;

namespace VoxelSystem
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    class BlockMarker : MonoBehaviour
    {
        [HideInInspector] public MeshRenderer meshRenderer;
        [HideInInspector] public MeshFilter meshFilter;
        [HideInInspector] public Mesh defaultMesh;           // Setup

        public BlockType blockType;
        public bool enableTransformation = true;

        void OnValidate()
        {
            if(meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();
        }
    }
}