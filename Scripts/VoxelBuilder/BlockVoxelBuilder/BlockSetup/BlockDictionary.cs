using System;
using System.Collections.Generic;
using System.Text;
using MUtility;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{
    [Serializable]
    public class BakedBlock
    {
        public List<Mesh> meshes;
    }
    
    [Serializable]
    public struct BlockKey
    {
        public BlockType blockType;
        public InVoxelDirection inVoxelDirection;
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
    
    [Serializable]
    public class BlockDictionary : SerializableDictionary<BlockKey, BakedBlock>
    {
        public void AddBlock(BlockKey key, Mesh mesh)
        {
            if (!ContainsKey(key))              
                Add(key, new BakedBlock { meshes = new List<Mesh>() });

            var meshList = this[key].meshes;
            if(!meshList.Contains(mesh))
                meshList.Add(mesh);
        }
    }
}