using MUtility;
using UnityEngine;

namespace VoxelSystem
{

	static class IntVoxelUtility
	{
		// Bits are numbered from left to right, starting from 0.

		// Fist   8 bits: Material index      (255 = empty)
		// Second 8 bits: Shape index
		// Third  8 bits: Extra data 1     - Can be used for anything
		// Fourth 8 bits: Extra data 2     - Can be used for anything
		//
		// Fourth byte can be used for: 
		// Transformation info
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

		// Extra data

		// Bits 24-31 are the additional info:
		internal static ushort GetExtraVoxelData(this int fullVoxelData) => (ushort)(fullVoxelData & 0xFFFF);
		internal static int SetExtraVoxelData(this ref int fullVoxelData, ushort newExtraValue) 
		{
			fullVoxelData &= ~0xFFFF;
			fullVoxelData |= (newExtraValue & 0xFFFF);
			return fullVoxelData;
		}

		internal static byte GetByte(this ushort data, int byteIndex) => (byte)((data >> (byteIndex * 8)) & 0xFF);
		internal static int Get4bit(this ushort data, int index) => (data >> (index * 4)) & 0xF;
		internal static int Get2Bit(this ushort data, int index) => (data >> (index * 2)) & 0b11;

		internal static ushort SetByte(this ref ushort data, int index, int newValue)
		{
			int shift = index * 8;
			int mask = ~(0xFF << shift);
			int shiftedValue = newValue << shift;

			data = (ushort)((data & mask) | shiftedValue);
			return data;
		}
		internal static ushort Set4Bit(this ref ushort data, int index, int newValue)
		{
			int shift = index * 4;
			int mask = ~(0xF << shift);
			int shiftedValue = newValue << shift;

			data = (ushort)((data & mask) | shiftedValue);
			return data;
		}
		internal static ushort Set2Bit(this ref ushort data, int index, int newValue)
		{
			int shift = index * 2;
			int mask = ~(0b11 << shift);
			int shiftedValue = newValue << shift;

			data = (ushort)((data & mask) | shiftedValue);
			return data;
		}

		// Additional 2 Bit // index: 0-3

		// ------------------------------------------------------------------------------------------------

		// Extra data used for transformational information

		// Bits 24-31 are the additional info
		//		Can be used fot anything
		//		Can be used fot transformation info

		//     X rotation is coded in bits: 16, 17
		
		internal static int GetXRotation(this ushort extraData) => extraData.Get2Bit(3);
		internal static ushort SetXRotation(this ref ushort extraData, int rotation) =>
			extraData.Set2Bit(3, rotation);

		//     Y rotation is coded in bits: 18, 19
		internal static int GetYRotation(this ushort extraData) => extraData.Get2Bit(2);
		internal static ushort SetYRotation(this ref ushort extraData, int rotation) =>
			extraData.Set2Bit(2, rotation);

		//     Z rotation is coded in bits: Bits 20, 21
		internal static int GetZRotation(this ushort extraData) => extraData.Get2Bit(1);
		internal static ushort SetZRotation(this ref ushort extraData, int rotation) =>
			extraData.Set2Bit(1, rotation);
		
		//     Flip: Bits 22, 23
		internal static Flip3D GetFlip(this ushort extraData) => (Flip3D)extraData.Get2Bit(0);
		internal static ushort SetFlip(this ref ushort extraData, Flip3D value) =>
			extraData.Set2Bit(0, (int)value); 

		internal static Vector3Int GetRotation(this ushort extraData) =>
			new(extraData.GetXRotation(), extraData.GetYRotation(), extraData.GetZRotation());

		internal static ushort SetRotation(this ref ushort extraData, Vector3Int rotation)
		{
			extraData.SetXRotation(rotation.x);
			extraData.SetYRotation(rotation.y);
			extraData.SetZRotation(rotation.z);
			return extraData;
		}
		internal static ushort SetRotation(this ref ushort extraData, int x, int y, int z)
		{
			extraData.SetXRotation(x);
			extraData.SetYRotation(y);
			extraData.SetZRotation(z);
			return extraData;
		}
		

	}
}
