using System;
using System.Collections.Generic;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{

    [Serializable]
    class MeshDictionary : SerializableDictionary<BlockType, Mesh> { }

    [Serializable]
    class TransformDictionary : SerializableDictionary<BlockType, TransformSet> { }
    
    [Serializable]
    struct TransformSet
    {
        public List< BlockTransformation> set;
    }
    
    
    [CreateAssetMenu(fileName = "DefaultBlockSetup", menuName = "VoxelSystem/DefaultBlockSetup")]
    class DefaultBlockSetup : ScriptableObject
    {
        [SerializeField] Material basicMaterial;
        [SerializeField] Material selectableMaterial;
        [SerializeField] Material selectedMaterial;
        
        [SerializeField] MeshDictionary meshDictionary = new ();
        [SerializeField] TransformDictionary transformDictionary = new ();
        
        public Mesh GetMesh(BlockType blockType) => meshDictionary[blockType];
        
        public Material GetBasicMaterial() => basicMaterial;
        public Material GetSelectableMaterial() => selectableMaterial;
        public Material GetSelectedMaterial() => selectedMaterial;
        
        int num = 0;

        public void AddBlock(BlockComponent block)
        {
            if (!transformDictionary.ContainsKey(block.blockType))
                transformDictionary.Add(block.blockType, new TransformSet {set = new List<BlockTransformation>()});

            var list = transformDictionary[block.blockType].set;
            if (!list.Contains(block.transformation))
            {
                Debug.Log(block.transformation, block.gameObject);
                list.Add(block.transformation);
            }
        }
    }
}