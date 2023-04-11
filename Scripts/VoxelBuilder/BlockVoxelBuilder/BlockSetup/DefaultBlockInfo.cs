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
    
    
    [CreateAssetMenu(fileName = "DefaultBlockInfo", menuName = "VoxelSystem/DefaultBlockInfo")]
    class DefaultBlockInfo : ScriptableObject
    {
        [SerializeField] Material basicMaterial;
        [SerializeField] Material testMaterial;
        [SerializeField] Material selectableMaterial;
        [SerializeField] Material selectedMaterial;
        
        [SerializeField] MeshDictionary meshDictionary = new ();
        [SerializeField] TransformDictionary transformDictionary = new ();
        
        
        static DefaultBlockInfo _instance;
        public static DefaultBlockInfo Instance
        {
            get
            {
                if(_instance == null)
                    _instance = Resources.Load<DefaultBlockInfo>("DefaultBlockInfo");

                if (_instance == null)
                    Debug.LogError("DefaultBlockInfo not found");
                
                return _instance;
            }
        }

        
        
        public Mesh GetMesh(BlockType blockType) => meshDictionary[blockType];
        
        public Material GetBasicMaterial() => basicMaterial;
        public Material GetSelectableMaterial() => selectableMaterial;
        public Material GetSelectedMaterial() => selectedMaterial;
        
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