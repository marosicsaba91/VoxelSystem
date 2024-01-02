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

		[FormerlySerializedAs("shapeID")] public int shapeId;
		public byte materialIndex;
		public byte closednessInfo;
		public byte cubicTransformation;
		public byte extraData;

		public Voxel(int shapeId, byte materialIndex, byte closednessInfo, byte cubicTransformation, byte extraData)
		{
			this.shapeId = shapeId;
			this.materialIndex = materialIndex;
			this.closednessInfo = closednessInfo;
			this.cubicTransformation = cubicTransformation;
			this.extraData = extraData;
		}

		public Voxel(long longData)
		{
			extraData = (byte)(longData & 0xFF);
			longData >>= 8;
			cubicTransformation = (byte)(longData & 0xFF);
			longData >>= 8;
			closednessInfo = (byte)(longData & 0xFF);
			longData >>= 8;
			materialIndex = (byte)(longData & 0xFF);
			longData >>= 8;
			shapeId = (int)longData;
		}

		public long ToLong()
		{
			long longData = shapeId;
			longData <<= 8;
			longData |= materialIndex;
			longData <<= 8;
			longData |= closednessInfo;
			longData <<= 8;
			longData |= cubicTransformation;
			longData <<= 8;
			longData |= extraData;
			return longData;
		}

		internal static Voxel emptyValue = new()
		{
			shapeId = 0,
			materialIndex = 0,
			extraData = 0,
			closednessInfo = 0,
		};

		public bool IsEmpty() => shapeId == 0;
		public bool IsFilled() => !IsEmpty();
		public bool IsFilled(GeneralDirection3D side) => !IsEmpty() && IsSideClosed(side);

		public static bool operator ==(Voxel a, Voxel b) =>
			a.shapeId == b.shapeId &&
			a.materialIndex == b.materialIndex &&
			a.closednessInfo == b.closednessInfo &&
			a.cubicTransformation == b.cubicTransformation &&
			a.extraData == b.extraData;

		public static bool operator !=(Voxel a, Voxel b) => !(a == b);
		public override bool Equals(object obj) => obj is Voxel other && this == other;
		public override int GetHashCode() => shapeId.GetHashCode() ^ materialIndex.GetHashCode() ^ closednessInfo.GetHashCode() ^ cubicTransformation.GetHashCode() ^ extraData.GetHashCode();

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