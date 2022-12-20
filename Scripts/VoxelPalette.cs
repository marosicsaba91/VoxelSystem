using System;
using UnityEngine; 
namespace VoxelSystem
{
    [CreateAssetMenu(fileName = "VoxelPalette", menuName = "VoxelSystem/VoxelPalette", order = 2)]
    public class VoxelPalette : ScriptableObject
    {
        public Material material;

        public struct PaletteItem
        {
            public int value;
            public string name;
            public Color color;
            public Sprite image;
        }

        public virtual PaletteItem[] GetPletteItems()
        {
            return Array.Empty<PaletteItem>();
        }

    }
}