using MUtility;
using UnityEngine;

namespace VoxelSystem
{
    [CreateAssetMenu(fileName = "VoxelMap", menuName = "VoxelSystem/VoxelMap", order = 1)]
    public class VoxelMapScriptableObject : ScriptableObject
    {
        [HideInInspector] public VoxelMap map;
        [HideInInspector] public OctVoxelMap2 octMap;

        [SerializeField] DisplayMember copy = new DisplayMember(nameof(Copy));

        void Copy()
        {
            octMap = new OctVoxelMap2(map.Size);
            for (int x = 0; x < map.Width; x++)
            for (int y = 0; y < map.Height; y++)
            for (int z = 0; z < map.Depth; z++)
                octMap.Set(x, y, z, map.Get(x,y,z).value);

        }
    }
}