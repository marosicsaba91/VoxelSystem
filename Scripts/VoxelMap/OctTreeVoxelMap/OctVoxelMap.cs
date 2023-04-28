using System;
using UnityEngine;
using MUtility;
using ProtoBuf;
using System.IO;

namespace VoxelSystem
{
	[Serializable]
	public partial class OctVoxelMap : VoxelMap<OctVoxelMap>, ISerializationCallbackReceiver
	{
		[SerializeField] Vector3Int canvasSize;
		[SerializeField] int levelCount;
		[SerializeField] byte[] data;

		OctVoxelChunk rootChunk;
		bool _serialized = false;

		public sealed override IntBounds VoxelBoundaries
		{
			get => new(Vector3Int.zero, canvasSize);
			protected set => Setup(value.Size); // RESET WHOLE MAP
		}
		public override Vector3Int FullSize
		{
			get
			{
				return Vector3Int.one * ChunkSizeLength;
			}
			protected set // RESET WHOLE MAP
			{
				Vector3Int full = value;
				if (value.x <= 0 || value.y <= 0 || value.z <= 0)
					full = Vector3Int.one * defaultCanvasSize;

				int longestCanvasSize = Mathf.Max(value.x, value.y, value.z);
				float log = Mathf.Log(longestCanvasSize, 2);
				levelCount = Mathf.CeilToInt(log); 
				canvasSize = value;
				rootChunk = new OctVoxelChunk();
				rootChunk.value = defaultValue;
			}
		}

		// Getters --------------------------------------------

		public OctVoxelChunk RootChunk => rootChunk;

		public int ChunkSizeLength
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

		public OctVoxelMap() { }

		public OctVoxelMap(int x, int y, int z) : this (new Vector3Int(x, y, z), defaultValue) { }

		public OctVoxelMap(Vector3Int canvasSize, int value = defaultValue)
		{
			Setup(canvasSize, value);
		}

		public sealed override void Setup() => Setup(defaultCanvasSize * Vector3Int.one);

		public sealed override void Setup(Vector3Int size, int value = defaultValue)
		{
			FullSize = size;			
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

		internal sealed override OctVoxelMap GetCopy() => new OctVoxelMap(this);

		// GET Voxels --------------------------------------------------------        

		public sealed override int GetVoxel(int x, int y, int z) => rootChunk.GetVoxel(x, y, z, ChunkSizeLength);

		// SET Voxels --------------------------------------------------------

		public override bool SetVoxel(int x, int y, int z, int value) => rootChunk.SetLeaf(x, y, z, value, ChunkSizeLength);

		public sealed override void SetWhole(int value)
		{
			rootChunk.Fill(value);
			MapChanged();
		}

		public sealed override void SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, SetAction action, int value)
		{
			// TODO: Very much not optimized

			IntBounds bounds = new(startCoordinate, endCoordinate + Vector3Int.one);
			bounds.Clamp(Vector3Int.zero, CanvasSize);

			if (action == SetAction.Set)
			{
				foreach (Vector3Int coordinate in bounds.WalkThrough())
					SetVoxel(coordinate, value);
			}
			if (action == SetAction.Repaint)
			{
				foreach (Vector3Int coordinate in bounds.WalkThrough())
					if (GetVoxel(coordinate).IsFilled())
						SetVoxel(coordinate, action, value);
			}
			else if (action == SetAction.Fill)
			{

				foreach (Vector3Int coordinate in bounds.WalkThrough())
					if (GetVoxel(coordinate).IsEmpty())
						SetVoxel(coordinate, action, value);
			}
			else if (action == SetAction.Clear)
			{
				foreach (Vector3Int coordinate in bounds.WalkThrough())
					SetVoxel(coordinate, action, IntVoxelUtility.emptyValue);
			}
		}

		// Serialization ----------------------------------------------------------

		protected override void OnMapChanged()
		{
			_serialized = false;
			SerializeToByeArray();
		}

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