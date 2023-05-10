using System;
using UnityEngine;
using MUtility;
using ProtoBuf;
using System.IO;

namespace VoxelSystem
{
	[Serializable]
	public partial class OctVoxelMap : VoxelMap, ISerializationCallbackReceiver
	{
		[SerializeField] Vector3Int canvasSize;
		[SerializeField] int levelCount;
		[SerializeField] byte[] data;

		OctVoxelChunk rootChunk;
		bool _serialized = false;

		public sealed override BoundsInt VoxelBoundaries
		{
			get => new(Vector3Int.zero, canvasSize);
			protected set => Setup(value.size); // RESET WHOLE MAP
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

		public OctVoxelMap() { SetupUniqueID(); }
		
		public OctVoxelMap(Vector3Int canvasSize, int value = defaultValue)
		{
			SetupUniqueID();
			Setup(canvasSize, value);
		}

		public sealed override void Setup() => Setup(defaultCanvasSize * Vector3Int.one);

		public sealed override void Setup(Vector3Int size, int value = defaultValue)
		{
			FullSize = size;			
			rootChunk.Fill(value);
		}

		// GET Voxels --------------------------------------------------------        

		public sealed override int GetVoxel(int x, int y, int z) => rootChunk.GetVoxel(x, y, z, ChunkSizeLength);

		// SET Voxels --------------------------------------------------------

		public override bool SetVoxel(int x, int y, int z, int value) => rootChunk.SetLeaf(x, y, z, value, ChunkSizeLength);

		public sealed override bool SetWhole(int value) => rootChunk.Fill(value); 

		public sealed override bool SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, VoxelAction action, int value)
		{
			// TODO: Very much not optimized
			BoundsInt bounds = new(startCoordinate, endCoordinate);
			bounds.Clamp(Vector3Int.zero, CanvasSize);
			bool changed = false;

			if (action == VoxelAction.Overwrite)
			{
				foreach (Vector3Int coordinate in bounds.WalkThrough())
					changed |= SetVoxel(coordinate, value);
			}
			if (action == VoxelAction.Repaint)
			{
				foreach (Vector3Int coordinate in bounds.WalkThrough())
					if (GetVoxel(coordinate).IsFilled())
						changed |= SetVoxel(coordinate, action, value);
			}
			else if (action == VoxelAction.Attach)
			{

				foreach (Vector3Int coordinate in bounds.WalkThrough())
					if (GetVoxel(coordinate).IsEmpty())
						changed |= SetVoxel(coordinate, action, value);
			}
			else if (action == VoxelAction.Erase)
			{
				foreach (Vector3Int coordinate in bounds.WalkThrough())
					changed |= SetVoxel(coordinate, action, IntVoxelUtility.emptyValue);
			}
			return changed;
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