using System;

namespace VoxelSystem
{
    public enum BlockType
    {
        SidePositive,
        EdgePositive,
        CornerPositive,

        EdgeNegative,
        CornerNegative,
        SideToPositiveEdge,
        SideToNegativeEdge,
        
        CrossCorner
    }
    
    static class BlockUtility
    {
        public static readonly BlockType[] allBlockType;

        static BlockUtility()
        {
            var all = Enum.GetValues(typeof(BlockType));
            allBlockType = new BlockType[all.Length];
            for (var i = 0; i < all.Length; i++)
                allBlockType[i] =(BlockType) all.GetValue(i);
        }
    }

}