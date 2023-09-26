using MUtility;
using UnityEngine;

namespace VoxelSystem
{

	static class IntVoxelUtility
	{
		// Bits are numbered from left to right, starting from 0.

		// Fist   8 bits: Material index      (255 = empty)
		// Second 8 bits: Shape index
		// Third  8 bits: NOT USED
		// Fourth  8 bits: Transformation info
		//		Rotate X:	 16, 17
		//		Rotate Y:	 18, 19
		//		Rotate Z:	 20, 21
		//		Flip:		 22, 23      0:Non   1:X   2:Y   3:Z

		// ------------------------------------------------------------------------------------------------

		// 0b 1111111 00000000 00000000 00000000
		public const int emptyValue = 255 << 24;

		// The first bit starting from left is the isEmpty flag

		public const int emptyMask = ~emptyValue; // 0b 00000000 1111111 1111111 1111111 


		internal static bool IsFilled(this int i) => (i & emptyValue) != emptyValue;
		internal static bool IsEmpty(this int i) => (i & emptyValue) == emptyValue;
		internal static void SetEmpty(this ref int i) => i = emptyValue;

		// ------------------------------------------------------------------------------------------------

		// Bits 0-7 are Material Index
		internal static byte GetMaterialIndex(this int i) =>
			(byte)((i >> 24) & 0xFF);

		internal static void SetMaterialIndex(this ref int i, byte materialIndex) =>
			i = (i & ~(0xFF << 24)) | (materialIndex << 24);

		// ------------------------------------------------------------------------------------------------

		// Bits 8-15 are the Shape Index

		internal static byte GetShapeIndex(this int i) =>
			(byte)((i >> 16) & 0xFF);

		internal static void SetShapeIndex(this ref int i, byte materialIndex) =>
			i = (i & ~(0xFF << 16)) | (materialIndex << 16);

		// ------------------------------------------------------------------------------------------------

		// Bits 24-31 are the transformation info

		//     X rotation is coded in bits: 16, 17
		static int GetXRotationIndex(this int i) => (i >> 6) & 0b11;
		static void SetXRotationIndex(this ref int i, int rotationIndex) => SetTwoBits(ref i, rotationIndex, 6);

		//     Y rotation is coded in bits: 18, 19
		static int GetYRotationIndex(this int i) => (i >> 4) & 0b11;
		static void SetYRotationIndex(this ref int i, int rotationIndex) => SetTwoBits(ref i, rotationIndex, 4);

		//     Z rotation is coded in bits: Bits 20, 21
		static int GetZRotationIndex(this int i) => (i >> 2) & 0b11;
		static void SetZRotationIndex(this ref int i, int rotationIndex) => SetTwoBits(ref i, rotationIndex, 2);

		//     Flip: Bits 22, 23
		internal static Flip3D GetFlip(this int i) => (Flip3D)(i & 0b11);
		internal static void SetFlip(this ref int i, Flip3D value) => SetTwoBits(ref i, (int)value, 0);


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


	}
}
