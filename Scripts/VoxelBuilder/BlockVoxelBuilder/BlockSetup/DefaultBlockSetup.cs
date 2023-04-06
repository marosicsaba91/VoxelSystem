using System;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{

    [Serializable]
    class MeshDictionary : SerializableDictionary<BlockType, Mesh> { }


    [CreateAssetMenu(fileName = "DefaultBlockSetup", menuName = "VoxelSystem/DefaultBlockSetup")]
    public class DefaultBlockSetup : ScriptableObject
    {
        [SerializeField] Material basicMaterial;
        [SerializeField] Material selectableMaterial;
        [SerializeField] Material selectedMaterial;
        [SerializeField] MeshDictionary meshDictionary = new ();
        
        public Mesh GetMesh(BlockType blockType) => meshDictionary[blockType];
        
        public Material GetBasicMaterial() => basicMaterial;
        public Material GetSelectableMaterial() => selectableMaterial;
        public Material GetSelectedMaterial() => selectedMaterial;
    }
}