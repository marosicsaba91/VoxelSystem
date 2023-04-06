using System;
using UnityEngine;
using Utility.SerializableCollection;
using VoxelSystem;

[Serializable]
class TransformDictionary : SerializableDictionary<Matrix4x4, bool> { }

[Serializable]

class OnBlockSetup
{
    [SerializeField] BlockType blockType;
    [SerializeField] Material material;
    [SerializeField] Mesh mesh;
}


[ExecuteAlways]
public class BlockSetup : MonoBehaviour
{
    [SerializeField] DefaultBlockSetup defaultBlockSetup;
    
    [Space]
    [SerializeField] BlockType blockType;
    [SerializeField] Material material;
    [SerializeField] Mesh mesh;
    [SerializeField] TransformDictionary transformDictionary = new ();

    
    void Update()
    {
        foreach (BlockMarker marker in GetComponentsInChildren<BlockMarker>())
        {
            marker.meshRenderer.material =
                blockType != marker.blockType ? defaultBlockSetup.GetBasicMaterial() :
                !marker.enableTransformation ? defaultBlockSetup.GetSelectableMaterial() :
                material != null ? material :
                defaultBlockSetup.GetSelectedMaterial();

            marker.meshFilter.mesh = blockType == marker.blockType && mesh != null && marker.enableTransformation
                ? mesh
                : defaultBlockSetup.GetMesh(marker.blockType);
        }
    }
}
