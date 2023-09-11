using UnityEngine;

namespace VoxelSystem
{
	static class IntVoxelUtility
	{
		// Bits are numbered from left to right, starting from 0.

		// Fist 8 bits have multiple purposes:
		//    First bit is the isEmpty flag      (minus numbers are empty)
		//    Second bit is the isFlipped flag
		//    Next6 bits are the rotation index.
		//      X: 2, 3
		//      Y: 4, 5
		//      Z: 6, 7

		// Second 8 bits are not used yet
		// Third 8 bits are shape index
		// Fourth 8 bits are the material index.


		// ------------------------------------------------------------------------------------------------

		public const int emptyValue = 1 << 31; // 0b 10000000 00000000 00000000 00000000

		// The first bit starting from left is the isEmpty flag

		public const int emptyMask = 1 << 31;    // 0b 10000000 00000000 00000000 00000000
		public const int fillMask = ~emptyMask;  // 0b 01111111 11111111 11111111 11111111


		internal static bool IsFilled(this int i) => i >= 0; // (i & fillMask) != 0;
		internal static bool IsEmpty(this int i) => i < 0; // (i & fillMask) == 0;
		internal static void SetEmpty(this ref int i) => i |= emptyMask;
		internal static void SetFilled(this ref int i) => i &= fillMask;
		internal static void SetFilled(this ref int i, bool value) =>
			i = value ?
			i & fillMask :   // Set 0 Filled
			i | emptyMask;     // Set 1 


		// ------------------------------------------------------------------------------------------------

		// The second bit starting from left is the isFlipped flag

		public const int flippedMask = 1 << 30; // 0b 01000000 00000000 00000000 00000000
		internal static bool IsFlipped(this int i) => (i >> 30 & 1) == 1;
		internal static void SetFlipped(this ref int i, bool value) =>
			i = value ?
			i | flippedMask :   // Set Flipped
			i & ~(1 << 30);     // Set Not Flipped


		// Next 6 bits are the rotation index.

		//     X rotation is coded in bits: 2, 3
		static int GetXRotationIndex(this int i) => (i >> 28) & 0x3;
		static void SetXRotationIndex(this ref int i, int rotationIndex) => SetTwoBits (ref i, rotationIndex, 28);

		//     Y rotation is coded in bits: 4, 5
		static int GetYRotationIndex(this int i) => (i >> 26) & 0x3;
		static void SetYRotationIndex(this ref int i, int rotationIndex) => SetTwoBits (ref i, rotationIndex, 26);

		//     Z rotation is coded in bits: 6, 7
		static int GetZRotationIndex(this int i) => (i >> 24) & 0x3;
		static void SetZRotationIndex(this ref int i, int rotationIndex) => SetTwoBits (ref i, rotationIndex, 24);

		static void SetTwoBits(this ref int i, int newValue, int shift)
		{
			newValue %= 4;
			if (newValue < 0)
				newValue += 4;

			int mask = ~(0b11 << shift);
			int shiftedValue = newValue << shift;

			i = (i & mask) | shiftedValue;
		}

		internal static Vector3Int GetRotation(this int i) =>
			new(GetXRotationIndex(i), GetYRotationIndex(i), GetZRotationIndex(i));

		internal static void SetRotation(this ref int i, Vector3Int rotation)
		{
			i.SetXRotationIndex(rotation.x);
			i.SetYRotationIndex(rotation.y);
			i.SetZRotationIndex(rotation.z);
		}
		internal static void SetRotation(this ref int i, int x, int y, int z)
		{
			i.SetXRotationIndex(x);
			i.SetYRotationIndex(y);
			i.SetZRotationIndex(z);
		}

		// ------------------------------------------------------------------------------------------------

		// Bits 8-15 are not used

		// ------------------------------------------------------------------------------------------------

		// Bits 16-23 are the Shape Index

		internal static byte GetShapeIndex(this int i) =>
			(byte)((i >> 8) & 0xFF);

		internal static void SetShapeIndex(this ref int i, byte materialIndex) =>
			i = (i & ~(0xFF << 8)) | (materialIndex << 8);

		// ------------------------------------------------------------------------------------------------

		// Bits 24-31 are Material Index

		internal static byte GetMaterialIndex(this int i) =>
			(byte)(i & 0xFF);

		internal static void SetMaterialIndex(this ref int i, byte materialIndex) =>
			i = (i & ~0xFF) | materialIndex;



	}
}
