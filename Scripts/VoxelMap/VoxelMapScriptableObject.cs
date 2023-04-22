using MUtility;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace VoxelSystem
{
    [CreateAssetMenu(fileName = "VoxelMap", menuName = "VoxelSystem/VoxelMap", order = 1)]
    public class VoxelMapScriptableObject : ScriptableObject
    {
        [HideInInspector] public VoxelMap map;
        public OctVoxelMap2 octMap;
        [SerializeField] VoxelMapScriptableObject copyTo;

        [Header("Testin")]
        [SerializeField] public Vector3Int testIndex;
        [SerializeField] DisplayMember copyToOctMap = new DisplayMember(nameof(CopyToOctMap));
        [SerializeField] DisplayMember copyOctMapToNormal = new DisplayMember(nameof(CopyOctMapToNormal));
        [SerializeField] DisplayMember test = new DisplayMember(nameof(Test));
        [SerializeField] DisplayMember WriteSummary = new DisplayMember(nameof(WriteAll));

        void CopyToOctMap()
        {
            octMap = new OctVoxelMap2(map.Size);
            for (int x = 0; x < map.Width; x++)
            for (int y = 0; y < map.Height; y++)
            for (int z = 0; z < map.Depth; z++)
                octMap.Set(x, y, z, map.Get(x,y,z).value);
        }

        void CopyOctMapToNormal()
        {
            VoxelMap map = new VoxelMap(octMap.Size, true);
            
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                    for (int z = 0; z < map.Depth; z++)
                        map.Set(x, y, z, octMap.Get(x, y, z));

            //map.FillRange(Vector3Int.one * 2, Vector3Int.one * 5, 1);
            
            copyTo.map.ClearWhole();
            copyTo.map.CopyFromOtherMap(map, Vector3Int.zero, Vector3Int.zero, map.Size);
        }


        void Test()
        {
            CopyToOctMap();
            int testResult = octMap.Get(testIndex.x, testIndex.y, testIndex.z);
            Debug.Log(testResult);
        }

        void WriteAll()
        {
            int level = octMap.levels;
            OctTreeNode chunk = octMap.rootChunk;
            chunk.WriteAll(level, "Root");
        }
    }
}