using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelSystem
{
    public enum SubVoxel
    {
        RightUpForward = 0,
        RightUpBackward = 1,
        RightDownForward = 2,
        RightDownBackward = 3,
        LeftUpForward = 4,
        LeftUpBackward = 5,
        LeftDownForward = 6,
        LeftDownBackward = 7,
    }


    [Flags]
    public enum SubVoxelFlags
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
        static readonly List <SubVoxelFlags> _allSubVoxel;   
        public static IReadOnlyList<SubVoxelFlags> AllSubVoxel => _allSubVoxel;       
        public static SubVoxelFlags None => 0;

        public static SubVoxelFlags All => (SubVoxelFlags)255;

        static SubVoxelUtility()
        {
            _allSubVoxel = Enum.GetValues(typeof(SubVoxelFlags)).Cast<SubVoxelFlags>().ToList(); 
        }  
    public static SubVoxelFlags FromVector(Vector3 v)
    {
        if (v.x >= 0)
        {
            if (v.y >= 0)
            {
                if (v.z >= 0)
                    return SubVoxelFlags.RightUpForward;
                return SubVoxelFlags.RightUpBackward;
            }

            if (v.z >= 0)
                return SubVoxelFlags.RightDownForward;
            return SubVoxelFlags.RightDownBackward;
        }

        if (v.y >= 0)
            if (v.z >= 0)
                return SubVoxelFlags.LeftUpForward;
            else
                return SubVoxelFlags.LeftUpBackward;
        if (v.z >= 0)
            return SubVoxelFlags.LeftDownForward;
        return SubVoxelFlags.LeftDownBackward;
    }
    
    public static Vector3Int ToVector(this SubVoxelFlags d) => d switch
    {
        SubVoxelFlags.RightUpForward => new Vector3Int(1, 1, 1),
        SubVoxelFlags.RightUpBackward => new Vector3Int(1, 1, -1),
        SubVoxelFlags.RightDownForward => new Vector3Int(1, -1, 1),
        SubVoxelFlags.RightDownBackward => new Vector3Int(1, -1, -1),
        SubVoxelFlags.LeftUpForward => new Vector3Int(-1, 1, 1),
        SubVoxelFlags.LeftUpBackward => new Vector3Int(-1, 1, -1),
        SubVoxelFlags.LeftDownForward => new Vector3Int(-1, -1, 1),
        SubVoxelFlags.LeftDownBackward => new Vector3Int(-1, -1, -1),
        _ => Vector3Int.zero
    };
}
}