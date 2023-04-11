using System;
using MUtility;
using UnityEngine;

namespace VoxelSystem
{
    [Serializable]
    public struct BlockTransformation 
    {
        [SerializeField] InVoxelDirection inVoxelDirection;
        [SerializeField] Vector3Int rotation90;   // 0, 1 = 90, 2 = 180, 3 = 270
        [SerializeField] Vector3Int scale;

        public InVoxelDirection InVoxelDirection
        {
            get => inVoxelDirection;
            set => inVoxelDirection = value;
        }
        
        public Vector3 InVoxelVector
        {
            get => inVoxelDirection.ToVector();
            set => inVoxelDirection = inVoxelDirection = VoxelUtility.FromVector(value);
        }
            

        public Vector3 Scale
        {
            get
            {
                Validate();
                return scale;
            }
            set
            {
                int x = Mathf.Clamp(Mathf.RoundToInt(value.x), -1, 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(value.y), -1, 1);
                int z = Mathf.Clamp(Mathf.RoundToInt(value.z), -1, 1);
                if(x == 0) scale.x = 1;
                if(y == 0) scale.y = 1;
                if(z == 0) scale.z = 1;
                scale = new Vector3Int(x, y, z);
            }
        }

        public Vector3 Rotation 
        {
            get
            {
                Validate();
                return rotation90 * 90;
            }
            set => rotation90 = new Vector3Int(
                    Mathf.RoundToInt(value.x / 90),
                    Mathf.RoundToInt(value.y / 90),
                    Mathf.RoundToInt(value.z / 90));
        }
        


        public void Validate()
        {
            rotation90.x = MathHelper.Mod(rotation90.x, 4);
            rotation90.y = MathHelper.Mod(rotation90.y, 4);
            rotation90.z = MathHelper.Mod(rotation90.z, 4);

            scale.x = Mathf.Clamp(scale.x, -1, 1);
            scale.y = Mathf.Clamp(scale.y, -1, 1);
            scale.z = Mathf.Clamp(scale.z, -1, 1);
            
            if(scale.x == 0) scale.x = 1;
            if(scale.y == 0) scale.y = 1;
            if(scale.z == 0) scale.z = 1;
        }

        public void SetPosition(Vector3 pos)
        { 
            inVoxelDirection = VoxelUtility.FromVector(ToInVoxelPos(pos));
        }

        static Vector3Int ToInVoxelPos(Vector3 pos) => new (ToInVoxelPos(pos.x), ToInVoxelPos(pos.y), ToInVoxelPos(pos.z));

        static int ToInVoxelPos(float x)
        {
            x = MathHelper.Mod(x, 1);
            return x < 0.5f ? 1 : -1;
        }
    }
}