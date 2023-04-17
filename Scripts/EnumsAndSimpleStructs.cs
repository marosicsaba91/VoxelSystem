using UnityEngine;
using MUtility;

namespace VoxelSystem
{ 

    public struct TextureQuad
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomLeft;
        public Vector2 bottomRight;

        public TextureQuad(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br)
        {
            topLeft = tl;
            topRight = tr;
            bottomLeft = bl;
            bottomRight = br;
        }
    } 
}