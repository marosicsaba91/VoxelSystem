//using ProtoBuf;

namespace VoxelSystem
{
	//[ProtoContract]
	public sealed class OctVoxelChunk : OctNode<int, OctVoxelChunk>
	{
		public const int defaultValue = -1;
		public override int DefaultValue => defaultValue;

		public override OctVoxelChunk CreateNew(int value) => new(value);

		public OctVoxelChunk(int value) : base(value) { }

		public OctVoxelChunk() : base(defaultValue) { }

		public override bool Equals(int a, int b) => a == b;
	}
}
