using UnityEngine;

namespace VoxelSystem
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    class BlockComponent : MonoBehaviour
    {
        [HideInInspector] public Transform transformCached;
        [HideInInspector] public MeshRenderer meshRenderer;
        [HideInInspector] public MeshFilter meshFilter;
         
        public BlockTransformation transformation;
        public BlockType blockType;

        public void Setup()
        {
            transformCached = GetComponent<Transform>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();

            transformation.Rotation = transformCached.rotation.eulerAngles;
            transformation.Scale = transformCached.lossyScale;
            transformation.SetPosition(transformCached.position);
        } 
    }
}