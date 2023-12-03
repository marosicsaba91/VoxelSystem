using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	static class SideClosenessHelper
	{
		public static bool IsSideClosed(this byte closednessInfo, GeneralDirection3D direction) 
		{
			int index = (int) direction;
			
			// Get bit at index
			return (closednessInfo & (1 << index)) != 0;		
		}
		
		public static void SetSideClosed(this ref byte closednessInfo, GeneralDirection3D direction, bool isClosed = true)
		{
			int index = (int) direction;
			
			// Set bit at index
			if (isClosed)
				closednessInfo |= (byte) (1 << index);
			else
				closednessInfo &= (byte) ~(1 << index);
		}

		public static void SetAllSidesClosed(this ref byte closednessInfo, bool isClosed = true)
		{
			if (isClosed)
				closednessInfo = byte.MaxValue;
			else
				closednessInfo = 0;
		}

		public static byte GetClosed(this byte[] closednessInfo, Vector3Int coordiante, Vector3Int fullSize) 
		{
			int index = ArrayVoxelMap.GetIndex(coordiante, fullSize);
			return closednessInfo[index];
		}

		public static void SetClosed(this byte[] closednessInfo, Vector3Int coordiante, Vector3Int fullSize, byte value)
		{
			int index = ArrayVoxelMap.GetIndex(coordiante, fullSize);
			closednessInfo[index] = value;
		}
	}
}
