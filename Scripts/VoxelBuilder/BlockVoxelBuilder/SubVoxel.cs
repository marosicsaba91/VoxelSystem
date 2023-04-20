using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelSystem
{
    [Flags]
    public enum SubVoxel
    {
        RightUpForward = 1,
        RightUpBackward = 2,
        RightDownForward = 4,
        RightDownBackward = 8,
        LeftUpForward = 16,
        LeftUpBackward = 32,
        LeftDownForward = 64,
        LeftDownBackward = 128,
    }
    

    public static class SubVoxelUtility
    { 
        static readonly List <SubVoxel> _allSubVoxel;   
        public static IReadOnlyList<SubVoxel> AllSubVoxel => _allSubVoxel;       
        public static SubVoxel None => 0;

        public static SubVoxel All => (SubVoxel)255;

        static SubVoxelUtility()
        {
            _allSubVoxel = Enum.GetValues(typeof(SubVoxel)).Cast<SubVoxel>().ToList(); 
        }  
    public static SubVoxel FromVector(Vector3 v)
    {
        if (v.x >= 0)
        {
            if (v.y >= 0)
            {
                if (v.z >= 0)
                    return SubVoxel.RightUpForward;
                return SubVoxel.RightUpBackward;
            }

            if (v.z >= 0)
                return SubVoxel.RightDownForward;
            return SubVoxel.RightDownBackward;
        }

        if (v.y >= 0)
            if (v.z >= 0)
                return SubVoxel.LeftUpForward;
            else
                return SubVoxel.LeftUpBackward;
        if (v.z >= 0)
            return SubVoxel.LeftDownForward;
        return SubVoxel.LeftDownBackward;
    }
    
    public static Vector3Int ToVector(this SubVoxel d) => d switch
    {
        SubVoxel.RightUpForward => new Vector3Int(1, 1, 1),
        SubVoxel.RightUpBackward => new Vector3Int(1, 1, -1),
        SubVoxel.RightDownForward => new Vector3Int(1, -1, 1),
        SubVoxel.RightDownBackward => new Vector3Int(1, -1, -1),
        SubVoxel.LeftUpForward => new Vector3Int(-1, 1, 1),
        SubVoxel.LeftUpBackward => new Vector3Int(-1, 1, -1),
        SubVoxel.LeftDownForward => new Vector3Int(-1, -1, 1),
        SubVoxel.LeftDownBackward => new Vector3Int(-1, -1, -1),
        _ => Vector3Int.zero
    };
}
}