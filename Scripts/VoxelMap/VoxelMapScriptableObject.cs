using MUtility;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
    [CreateAssetMenu(fileName = "VoxelMap", menuName = "VoxelSystem/VoxelMap", order = 1)]
    public class VoxelMapScriptableObject : ScriptableObject
    {
        public VoxelMap map;
        public OctTree octMap;

        [SerializeField] DisplayMember copyToOctMap = new DisplayMember(nameof(CopyToOctMap)); 

        void CopyToOctMap()
        {
            octMap = new OctTree(map.Size);
            for (int x = 0; x < map.Width; x++)
            for (int y = 0; y < map.Height; y++)
            for (int z = 0; z < map.Depth; z++)
                octMap.Set(x, y, z, map.Get(x,y,z).value);

            // Make ScriptableObject dirty
            EditorUtility.SetDirty(this);

        }


    }
}