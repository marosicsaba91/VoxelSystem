using System;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public sealed class OctVoxelMap_Legacy : OctTree<OctVoxelChunk_Legacy, int>
	{
		[SerializeField] byte[] data;

		const int defaultCanvasSize = 8;
		public override int DefaultValue => OctVoxelChunk_Legacy.defaultValue;
		public override OctVoxelChunk_Legacy CreateNewNode(int value) => new(value);

		public OctVoxelMap_Legacy(int value) : base(Vector3Int.one * defaultCanvasSize, value) { }
		public OctVoxelMap_Legacy(Vector3Int canvasSize) : base(canvasSize, OctVoxelChunk_Legacy.defaultValue) { }
		public OctVoxelMap_Legacy(int canvasSize, int value) : base(Vector3Int.one * canvasSize, value) { }
		public OctVoxelMap_Legacy(int x, int y, int z, int value) : base(new Vector3Int(x, y, z), value) { }
		public OctVoxelMap_Legacy(Vector3Int canvasSize, int value) : base(canvasSize, value) { }

	}
}