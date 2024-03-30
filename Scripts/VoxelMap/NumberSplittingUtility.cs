
namespace VoxelSystem
{	static class NumberSplittingUtility
	{ 
		internal static byte GetByte(this ushort data, int byteIndex) =>
			(byte)((data >> (byteIndex * 8)) & 0xFF);
		internal static int Get4bit(this byte data, int index) => (data >> (index * 4)) & 0xF;
		internal static int Get2Bit(this byte data, int index) => (data >> (index * 2)) & 0b11;

		internal static ushort SetByte(this ref ushort data, int index, int newValue)
		{
			int shift = index * 8;
			int mask = ~(0xFF << shift);
			int shiftedValue = newValue << shift;

			data = (ushort)((data & mask) | shiftedValue);
			return data;
		}
		internal static byte Set4Bit(this ref byte data, int index, int newValue)
		{
			int shift = index * 4;
			int mask = ~(0xF << shift);
			int shiftedValue = newValue << shift;

			data = (byte)((data & mask) | shiftedValue);
			return data;
		}
		internal static byte Set2Bit(this ref byte data, int index, int newValue)
		{
			int shift = index * 2;
			int mask = ~(0b11 << shift);
			int shiftedValue = newValue << shift;

			data = (byte)((data & mask) | shiftedValue);
			return data;
		}
	}
}
