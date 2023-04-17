using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;

namespace VoxelSystem
{
    public static class BlockVoxelUtility
    {
        static readonly List <InVoxelDirection> _allInVoxelDirection; 
        static readonly List<BlockType> _allBlockType;
        static readonly List<Axis3D> _allAxis;
        public static IReadOnlyList<InVoxelDirection> AllInVoxelDirection => _allInVoxelDirection;
        public static IReadOnlyList<BlockType> AllBlockType => _allBlockType;
        public static IReadOnlyList<Axis3D> AllAxis => _allAxis;

        static BlockVoxelUtility()
        {
            _allBlockType = Enum.GetValues(typeof(BlockType)).Cast<BlockType>().ToList();
            _allInVoxelDirection = Enum.GetValues(typeof(InVoxelDirection)).Cast<InVoxelDirection>().ToList();
            _allAxis = Enum.GetValues(typeof(Axis3D)).Cast<Axis3D>().ToList();
        }



        public static bool HaveAxis(this BlockType blockType)
        {
            if (blockType == BlockType.CornerPositive) return false; 
            if (blockType == BlockType.CornerNegative) return false;
            if (blockType == BlockType.CrossCorner) return false;

            return true;
        }
        public static Vector3Int ToVector3Int(Vector3 v) => new Vector3Int(v.x >= 0 ? 1 : -1, v.y >= 0 ? 1 : -1, v.z >= 0 ? 1 : -1);
    }
}