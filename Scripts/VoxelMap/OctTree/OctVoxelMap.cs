using System;
using UnityEngine; 

namespace VoxelSystem
{
	[Serializable]
	public sealed class OctVoxelMap : OctTree<OctVoxelChunk, int>, ISerializationCallbackReceiver
	{ 
		const int defaultCanvasSize = 8;
		public override int DefaultValue => OctVoxelChunk.defaultValue;
		public override OctVoxelChunk CreateNewNode(int value) => new OctVoxelChunk(value);

		public OctVoxelMap(int value) : base( Vector3Int.one * defaultCanvasSize, value) { }
		public OctVoxelMap(Vector3Int canvasSize) : base(canvasSize, OctVoxelChunk.defaultValue) { }
		public OctVoxelMap(int canvasSize, int value) : base(Vector3Int.one * canvasSize, value) { }
		public OctVoxelMap(int x, int y, int z, int value) : base(new Vector3Int(x, y, z), value) { }
		public OctVoxelMap(Vector3Int canvasSize, int value) : base(canvasSize, value) { }

		// ----------------------------------------------------------

		//static BinaryFormatter formatter = new BinaryFormatter();
		//static MemoryStream stream = new MemoryStream();
		public void OnBeforeSerialize()
		{

			//Debug.Log($"OnBeforeSerialize:  {rootChunk.ChunkCount}");
			//stream.Position = 0; 
			//formatter.Serialize(stream, rootChunk);
			//data = stream.ToArray();
		}

		public void OnAfterDeserialize()
		{
			//Debug.Log("OnAfterDeserialize");
			//stream.Position = 0;
			//stream.Write(data, 0, data.Length);
			//stream.Position = 0;
			//rootChunk = (OctTreeNode2)formatter.Deserialize(stream);
		}
	}
}