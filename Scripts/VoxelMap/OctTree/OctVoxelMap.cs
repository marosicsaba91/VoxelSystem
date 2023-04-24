// using ProtoBuf;
using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine; 

namespace VoxelSystem
{
	[Serializable]
	public sealed class OctVoxelMap : OctTree<OctVoxelChunk, int>, ISerializationCallbackReceiver
	{
		[SerializeField] byte[] data;

		const int defaultCanvasSize = 8;
		public override int DefaultValue => OctVoxelChunk.defaultValue;
		public override OctVoxelChunk CreateNewNode(int value) => new(value);

		public OctVoxelMap(int value) : base( Vector3Int.one * defaultCanvasSize, value) { }
		public OctVoxelMap(Vector3Int canvasSize) : base(canvasSize, OctVoxelChunk.defaultValue) { }
		public OctVoxelMap(int canvasSize, int value) : base(Vector3Int.one * canvasSize, value) { }
		public OctVoxelMap(int x, int y, int z, int value) : base(new Vector3Int(x, y, z), value) { }
		public OctVoxelMap(Vector3Int canvasSize, int value) : base(canvasSize, value) { }

		// ----------------------------------------------------------
		 
		// static readonly MemoryStream stream = new(data);

		public void OnBeforeSerialize()
		{
			//Debug.Log($"OnBeforeSerialize:  {rootChunk.ChunkCount}");

			using (MemoryStream stream = new(data, true)) 
			{
				// Serializer.Serialize(stream, rootChunk);
			}
		
		}

		public void OnAfterDeserialize()
		{
			//Debug.Log("OnAfterDeserialize");

			using (MemoryStream stream = new(data, false))
			{
				// rootChunk = Serializer.Deserialize<OctVoxelChunk>(stream);
			}
		}
	}
}