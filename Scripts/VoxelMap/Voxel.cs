using MUtility;
using System;
using UnityEngine.Serialization;

namespace VoxelSystem
{
	[Serializable]
	public struct Voxel 
	{
		// The sequence of variables is NOT rearrangeable:
		// The type is exactly 8 byte now, but it can grow to 12 byte with the wrong order

		[FormerlySerializedAs("shapeID")] public uint shapeId;
		public byte materialIndex;
		public byte closednessInfo;
		public ushort extraVoxelData;

		public Voxel(uint shapeId, byte materialIndex, ushort extraVoxelData, byte closednessInfo)
		{
			this.shapeId = shapeId;
			this.materialIndex = materialIndex;
			this.extraVoxelData = extraVoxelData;
			this.closednessInfo = closednessInfo;
		}

		public Voxel(long longData)
		{
			closednessInfo = (byte)(longData & 0xFF);
			longData >>= 8;
			extraVoxelData = (ushort)(longData & 0xFFFF);
			longData >>= 16;
			materialIndex = (byte)(longData & 0xFF);
			longData >>= 8;
			shapeId = (uint)longData;
		}

		public long ToLong()
		{
			long longData = shapeId;
			longData <<= 8;
			longData |= materialIndex;
			longData <<= 16;
			longData |= extraVoxelData;
			longData <<= 8;
			longData |= closednessInfo;
			return longData;
		}

		internal static Voxel emptyValue = new()
		{
			shapeId = 0,
			materialIndex = 0,
			extraVoxelData = 0,
			closednessInfo = 0,
		};

		public bool IsEmpty() => shapeId == 0;
		public bool IsFilled() => !IsEmpty();
		public bool IsFilled(GeneralDirection3D side) => !IsEmpty() && IsSideClosed(side);

		public static bool operator ==(Voxel a, Voxel b) => a.shapeId == b.shapeId && a.materialIndex == b.materialIndex && a.extraVoxelData == b.extraVoxelData && a.closednessInfo == b.closednessInfo;
		public static bool operator !=(Voxel a, Voxel b) => !(a == b);
		public override bool Equals(object obj) => obj is Voxel other && this == other;
		public override int GetHashCode() => shapeId.GetHashCode() ^ materialIndex.GetHashCode() ^ extraVoxelData.GetHashCode() ^ closednessInfo.GetHashCode();

		public bool IsSideClosed(GeneralDirection3D side) => 
			(closednessInfo & (1 << (int)side)) != 0;

		public void SetSideClosed(GeneralDirection3D side, bool closed)
		{
			if (closed)
				closednessInfo |= (byte)(1 << (int)side);
			else
				closednessInfo &= (byte)~(1 << (int)side);
		}

		public void CloseAllSide() => closednessInfo = byte.MaxValue;
		public void OpenAllSide() => closednessInfo = 0;
		public void SetAllSideClose(bool closed) => closednessInfo = closed ? byte.MaxValue : (byte)0;
	}
}