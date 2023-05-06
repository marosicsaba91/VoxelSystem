// using ProtoBuf;
using UnityEngine;

namespace VoxelSystem
{
	//[ProtoContract]
	public sealed class OctVoxelChunk_Legacy : OctNode<int, OctVoxelChunk_Legacy>
	{
		public const int defaultValue = -1;
		public override int DefaultValue => defaultValue;


		//[ProtoMember(1)]
		public new int Value
		{
			get => value;
			set => this.value = value;
		}

		//[ProtoMember(2)]
		public OctVoxelChunk_Legacy[] InnerChunks
		{
			get => innerChunks;
			set => innerChunks = value;
		}

		public override OctVoxelChunk_Legacy CreateNew(int value) => new(value);

		public OctVoxelChunk_Legacy(int value) : base(value) { }

		public OctVoxelChunk_Legacy() : base(defaultValue) { }

		public override bool Equals(int a, int b) => a == b;
	}
}
