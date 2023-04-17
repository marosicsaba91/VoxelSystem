using System;
using System.Text;
using MUtility;
using UnityEngine;
using UnityEngine.Serialization;

namespace VoxelSystem
{
    [Serializable]
    public struct BlockKey
    {
        public BlockType blockType;
        [FormerlySerializedAs("subVoxelDirection")] public InVoxelDirection inVoxelDirection;
        public Axis3D axis;
        public Vector3Int doubleSize;
            
        public BlockKey(BlockType blockType, InVoxelDirection inVoxelDirection, Axis3D axis, Vector3Int doubleSize)
        {
            this.blockType = blockType;
            this.inVoxelDirection = inVoxelDirection;
            this.doubleSize = doubleSize;
            this.axis = axis;
        }
        
        public BlockKey(BlockType blockType, InVoxelDirection inVoxelDirection, Axis3D axis)
        {
            this.blockType = blockType;
            this.inVoxelDirection = inVoxelDirection;
            this.axis = axis;
            doubleSize = Vector3Int.one;
        }

        public void AppendTo(StringBuilder builder)
        {
            builder.Append(blockType);
            builder.Append("\t");
            builder.Append(inVoxelDirection);
            if (blockType.HaveAxis())
            {
                builder.Append("\t");
                builder.Append(axis);
            }
        }

    }

}