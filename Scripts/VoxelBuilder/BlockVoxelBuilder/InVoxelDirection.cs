using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelSystem
{
    public enum InVoxelDirection
    {
        RightUpForward,
        RightUpBackward,
        RightDownForward,
        RightDownBackward,
        LeftUpForward,
        LeftUpBackward,
        LeftDownForward,
        LeftDownBackward
    }

    public static class SubVoxelUtility
    {
        static readonly List <InVoxelDirection> _allInVoxelDirection;  
        static readonly List <SubVoxel> _allSubVoxel;  
        public static IReadOnlyList<InVoxelDirection> AllInVoxelDirection => _allInVoxelDirection;
        public static IReadOnlyList<SubVoxel> AllSubVoxel => _allSubVoxel; 

        static SubVoxelUtility()
        {
            _allSubVoxel = Enum.GetValues(typeof(SubVoxel)).Cast<SubVoxel>().ToList();
            _allInVoxelDirection = Enum.GetValues(typeof(InVoxelDirection)).Cast<InVoxelDirection>().ToList(); 
        }
        
        
        public static SubVoxel ToNewVersion(this InVoxelDirection ivd) => ivd switch
        {
            InVoxelDirection.RightUpForward => SubVoxel.RightUpForward,
            InVoxelDirection.RightUpBackward => SubVoxel.RightUpBackward,
            InVoxelDirection.RightDownForward => SubVoxel.RightDownForward,
            InVoxelDirection.RightDownBackward => SubVoxel.RightDownBackward,
            InVoxelDirection.LeftUpForward => SubVoxel.LeftUpForward,
            InVoxelDirection.LeftUpBackward => SubVoxel.LeftUpBackward,
            InVoxelDirection.LeftDownForward => SubVoxel.LeftDownForward,
            InVoxelDirection.LeftDownBackward => SubVoxel.LeftDownBackward,
            _ => default
        };

        public static Vector3Int ToVector(this InVoxelDirection d) => d switch
        {
            InVoxelDirection.RightUpForward => new Vector3Int(1, 1, 1),
            InVoxelDirection.RightUpBackward => new Vector3Int(1, 1, -1),
            InVoxelDirection.RightDownForward => new Vector3Int(1, -1, 1),
            InVoxelDirection.RightDownBackward => new Vector3Int(1, -1, -1),
            InVoxelDirection.LeftUpForward => new Vector3Int(-1, 1, 1),
            InVoxelDirection.LeftUpBackward => new Vector3Int(-1, 1, -1),
            InVoxelDirection.LeftDownForward => new Vector3Int(-1, -1, 1),
            InVoxelDirection.LeftDownBackward => new Vector3Int(-1, -1, -1),
            _ => Vector3Int.zero
        };

        public static InVoxelDirection FromVector(Vector3 v)
        {
            if (v.x >= 0)
            {
                if (v.y >= 0)
                {
                    if (v.z >= 0)
                        return InVoxelDirection.RightUpForward;
                    return InVoxelDirection.RightUpBackward;
                }

                if (v.z >= 0)
                    return InVoxelDirection.RightDownForward;
                return InVoxelDirection.RightDownBackward;
            }

            if (v.y >= 0)
                if (v.z >= 0)
                    return InVoxelDirection.LeftUpForward;
                else
                    return InVoxelDirection.LeftUpBackward;
            if (v.z >= 0)
                return InVoxelDirection.LeftDownForward;
            return InVoxelDirection.LeftDownBackward;
        }

    }

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
}