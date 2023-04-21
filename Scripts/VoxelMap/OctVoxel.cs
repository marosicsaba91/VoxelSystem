using System;
using UnityEngine;

namespace VoxelSystem
{
    [Serializable]
    public struct OctVoxel
    {
        /// <summary>
        /// If value == int.MaxValue, the voxel is clear.
        /// If value > 0, than MaterialIndex = value;
        /// If value < 0, the voxel is not fully clear nor filled. The absolute value of value is the size of the chunk.
        /// </summary>
        [HideInInspector] int value;
        public const int emptyValue = int.MaxValue;

        public bool IsEmpty => value < 0;
        public bool IsFilled => value == emptyValue;
        public bool IsMixed => value < 0;
        public void Clear() => value = emptyValue;
        public void Set(int value) => this.value = value;

        public OctVoxel(int materialIndex = emptyValue) =>
            value = materialIndex;

        public int Value
        {
            get => value >= 0 ? value : -1;
            set => this.value = Mathf.Max(0, value);
        }


        public int ChunkSize
        {
            get => value >= 0 ? 1 : -value;
            set => this.value = -value;
        }
    }
}

