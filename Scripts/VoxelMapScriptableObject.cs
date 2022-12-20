using UnityEngine;

namespace VoxelSystem
{
    [CreateAssetMenu(fileName = "VoxelMap", menuName = "VoxelSystem/VoxelMap", order = 1)]
    public class VoxelMapScriptableObject : ScriptableObject
    {
        [HideInInspector] public VoxelMap map;
    }
}