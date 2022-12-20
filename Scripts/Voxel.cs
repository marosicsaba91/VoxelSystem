using System;
using UnityEngine;

namespace VoxelSystem
{
    [Serializable]
    public struct Voxel
    {
        /// <summary>
        /// If 0, the voxel is clear. If value > 0, than MaterialIndex = value-1;
        /// </summary>
        [HideInInspector] public int value;

        public bool IsEmpty => value < 0;
        public bool IsFilled => value >= 0;
        internal void Clear() => value = -1;

        public Voxel(int materialIndex = -1) =>
            value = materialIndex;
    }
}