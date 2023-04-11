using System; 
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{

    [Serializable]
    class EnabledTransformDictionary : SerializableDictionary<BlockTransformation, bool> { }
 
    public class BlockSetup : MonoBehaviour
    {
        [SerializeField] DefaultBlockSetup defaultBlockSetup;

        [Space] 
        public BlockType blockType;
        public Mesh mesh;
        
        [SerializeField] EnabledTransformDictionary transformations = new();

        public bool IsTransformEnabled(BlockTransformation t) => 
            transformations.TryGetValue(t, out bool value) && value; 
    }
}