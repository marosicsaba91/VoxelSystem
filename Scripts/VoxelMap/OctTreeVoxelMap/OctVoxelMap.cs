using System;
using UnityEngine;
using MUtility;
using ProtoBuf;
using System.IO;

namespace VoxelSystem
{
	[Serializable]
	public partial class OctVoxelMap : VoxelMap<int>, ISerializationCallbackReceiver
	{
		[SerializeField] Vector3Int canvasSize;
		[SerializeField] int levelCount;
		[SerializeField] byte[] data;

		OctVoxelChunk rootChunk;
		bool _serialized = false;

		public override IntBounds VoxelBoundaries => new(Vector3Int.zero, canvasSize);

		public override bool IsVoxelFilled(int v) => v.IsFilled();

		// Getters --------------------------------------------

		public OctVoxelChunk RootChunk => rootChunk;

		public int ChunkSize
		{
			get
			{
				int s = 1;
				for (int i = 0; i < levelCount; i++)
					s *= 2;
				return s;
			}
		}

		public int LevelCount => levelCount;

		public Vector3Int CanvasSize => canvasSize; 

		public int Width => canvasSize.x;
		public int Height => canvasSize.y;
		public int Depth => canvasSize.z;

		public object FullChunkCount => rootChunk.ChunkCount;

		// Constructors -----------------------------------------------------------------------------

		const int defaultValue = OctVoxelChunk.defaultValue;
		const int defaultCanvasSize = 8;

		public OctVoxelMap() : this(Vector3Int.one * defaultCanvasSize) { }

		public OctVoxelMap(int x, int y, int z) : this (new Vector3Int(x, y, z), defaultValue) { }

		public OctVoxelMap(Vector3Int canvasSize, int value = defaultValue)
		{

			if (canvasSize.x <= 0 || canvasSize.y <= 0 || canvasSize.z <= 0)
				canvasSize = Vector3Int.one * defaultCanvasSize;

			int longestCanvasSize = Mathf.Max(canvasSize.x, canvasSize.y, canvasSize.z);
			float log = Mathf.Log(longestCanvasSize, 2);
			levelCount = Mathf.CeilToInt(log);
			this.canvasSize = canvasSize;
			rootChunk = new OctVoxelChunk();
			rootChunk.Fill(value);
		}

		public OctVoxelMap(OctVoxelMap original) // Copy constructor
		{
			canvasSize = original.canvasSize;
			levelCount = original.levelCount;
			data = new byte[data.Length];
			data.CopyTo(original.data, 0);

			_serialized = false;
		}

		internal override VoxelMap GetCopy() => new OctVoxelMap(this);

		// GET Voxels --------------------------------------------------------        

		public override int GetVoxel(int x, int y, int z) => rootChunk.GetVoxel(x, y, z, ChunkSize); 

		// SET Voxels --------------------------------------------------------


		public bool Set(int x, int y, int z, SetAction action, int value)
		{
			if (action == SetAction.Repaint)
			{
				int v = rootChunk.GetVoxel(x, y, z, ChunkSize);
				if (v == defaultValue )
					return false;
			}

			bool changed = rootChunk.SetLeaf(x, y, z, value, ChunkSize);

			if(changed)
				MapChanged();

			return changed;
		}

		public bool Set(Vector3Int coordinate, SetAction action, int value) => Set(coordinate.x, coordinate.y, coordinate.z, action, value);

		public bool Set(Vector3Int coordinate, int value) => Set(coordinate.x, coordinate.y, coordinate.z, SetAction.Fill, value);

		public bool Set(int x, int y, int z, int value) => Set(x, y, z, SetAction.Fill, value);

		public bool Repaint(Vector3Int coordinate, int value) => Set(coordinate.x, coordinate.y, coordinate.z, SetAction.Repaint, value);

		public bool Repaint(int x, int y, int z, int value) => Set(x, y, z, SetAction.Repaint, value);


		public void ClearWhole(Vector3Int canvasSize)
		{
			this.canvasSize = canvasSize;
			rootChunk.Fill(defaultValue);
			MapChanged();
		}

		public void FillWhole(int value)
		{
			rootChunk.Fill(value);
			MapChanged();
		}

		// Serialization ----------------------------------------------------------

		public void OnBeforeSerialize() => SerializeToByeArray();
		public void OnAfterDeserialize() => DeserializeFromByteArray();

		internal void SerializeToByeArray()
		{
			if (_serialized) return;

			using (MemoryStream stream = new())
			{
				Serializer.Serialize(stream, rootChunk);
				data = stream.ToArray();
				Debug.Log("Serialized!   Bytes: " + data.Length);
			}
			_serialized = true;			
		}

		internal void DeserializeFromByteArray()
		{
			if (data.IsNullOrEmpty()) return;

			using (MemoryStream stream = new(data, false))
			{
				rootChunk = Serializer.Deserialize<OctVoxelChunk>(stream);
			}
		}
	}
}