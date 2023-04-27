using UnityEngine;

namespace VoxelSystem
{
	static class IntVoxelUtility
	{
		public const int emptyValue = -1;
		internal static bool IsFilled(this int i) => i != emptyValue;

		internal static bool IsEmpty(this int i) => i < 0;

		internal static void Clear(this ref int i) => i = -1;

	}
}
