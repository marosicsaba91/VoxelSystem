using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
    [CreateAssetMenu(fileName = "BlockLibrary", menuName = "VoxelSystem/BlockLibrary")]
    public class BlockLibrary : ScriptableObject
    {
        [SerializeField] Material material;
        [SerializeField] List<BlockSetup> blockSetups;
    }
}