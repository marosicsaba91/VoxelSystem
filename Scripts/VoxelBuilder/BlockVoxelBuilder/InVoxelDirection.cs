using UnityEngine;

namespace VoxelSystem
{
    public enum InVoxelDirection
    {
        RightUpForward,
        RightUpBackward,
        RightDownForward,
        RightDownBackward,
        LeftUpForward,
        LeftUpBackward,
        LeftDownForward,
        LeftDownBackward
    }

    public static class InVoxelDirectionUtility
    {
        public static Vector3Int ToVector(this InVoxelDirection d)
        {
            if (d == InVoxelDirection.RightUpForward) return new Vector3Int(1, 1, 1);
            if (d == InVoxelDirection.RightUpBackward) return new Vector3Int(1, 1, -1);
            if (d == InVoxelDirection.RightDownForward) return new Vector3Int(1, -1, 1);
            if (d == InVoxelDirection.RightDownBackward) return new Vector3Int(1, -1, -1);
            if (d == InVoxelDirection.LeftUpForward) return new Vector3Int(-1, 1, 1);
            if (d == InVoxelDirection.LeftUpBackward) return new Vector3Int(-1, 1, -1);
            if (d == InVoxelDirection.LeftDownForward) return new Vector3Int(-1, -1, 1);
            if (d == InVoxelDirection.LeftDownBackward) return new Vector3Int(-1, -1, -1);
            return Vector3Int.zero;
        }

        public static InVoxelDirection FromVector(Vector3 v)
        {
            if (v.x >= 0)
            {
                if (v.y >= 0)
                {
                    if (v.z >= 0)
                        return InVoxelDirection.RightUpForward;
                    return InVoxelDirection.RightUpBackward;
                }

                if (v.z >= 0)
                    return InVoxelDirection.RightDownForward;
                return InVoxelDirection.RightDownBackward;
            }

            if (v.y >= 0)
                if (v.z >= 0)
                    return InVoxelDirection.LeftUpForward;
                else
                    return InVoxelDirection.LeftUpBackward;
            if (v.z >= 0)
                return InVoxelDirection.LeftDownForward;
            return InVoxelDirection.LeftDownBackward;
        }

        public static Vector3Int ToVector3Int(Vector3 v) => new Vector3Int(v.x >= 0 ? 1 : -1, v.y >= 0 ? 1 : -1, v.z >= 0 ? 1 : -1);
    }
}