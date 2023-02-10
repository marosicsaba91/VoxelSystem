using System;
using UnityEngine;

namespace VoxelSystem
{
[Serializable]
public struct PaletteItem
{
    public int value;
    public string name;
    public Color color;
    public Sprite image;
}
}