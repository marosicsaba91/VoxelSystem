using System;
using UnityEngine;
using MUtility;
using ProtoBuf;
using System.IO;

namespace VoxelSystem
{
	// This version of OctTree is not used anymore, because it's inefficient 

	[Serializable]
	public class OctVoxelMap : ISerializationCallbackReceiver
	{
		[SerializeField] Vector3Int canvasSize;
		[SerializeField] int levelCount;
		[SerializeField] byte[] data;

		OctVoxelChunk rootChunk;
		bool _serialized = false;


		// Actions --------------------------------------------

		public event Action MapChangedEvent;

		void MapChanged()
		{
			_serialized = false;
			MapChangedEvent?.Invoke();
		}

		public void UndoRedoEvenInvokedOnMap() =>
			MapChangedEvent?.Invoke();

		// Getters --------------------------------------------

		public OctVoxelChunk RootChunk => rootChunk;

		public int RealSize
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

		public int GetSize(Axis3D a)
		{
			if (a == Axis3D.X)
				return canvasSize.x;
			if (a == Axis3D.Y)
				return canvasSize.y;
			return canvasSize.z;
		}

		public int Width => canvasSize.x;
		public int Height => canvasSize.y;
		public int Depth => canvasSize.z;

		public object FullChunkCount => rootChunk.ChunkCount;

		public bool IsValidCoord(int x, int y, int z)
		{
			return
				x >= 0 && x < canvasSize.x &&
				y >= 0 && y < canvasSize.y &&
				z >= 0 && z < canvasSize.z;
		}

		public bool IsValidCoord(Vector3Int coordinate) => IsValidCoord(coordinate.x, coordinate.y, coordinate.z);

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

		// GET Voxels --------------------------------------------------------        

		public int Get(int x, int y, int z) => rootChunk.GetLeaf(x, y, z, RealSize);

		public bool IsFilledSafe(Vector3Int index) => index.x >= 0 && index.x < canvasSize.x &&
				   index.y >= 0 && index.y < canvasSize.y &&
				   index.z >= 0 && index.z < canvasSize.z &&
				   Get(index.x, index.y, index.z) != defaultValue;

		// SET Voxels --------------------------------------------------------

		public enum SetAction { Fill, Repaint }

		public bool Set(int x, int y, int z, SetAction action, int value)
		{
			if (action == SetAction.Repaint)
			{
				int v = rootChunk.GetLeaf(x, y, z, RealSize);
				if (v == defaultValue )
					return false;
			}

			bool changed = rootChunk.SetLeaf(x, y, z, value, RealSize);

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

		public void OnBeforeSerialize() => Serialize();
		public void OnAfterDeserialize() => Deserialize();

		public void Serialize()
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

		public void Deserialize()
		{
			if (data.IsNullOrEmpty()) return;

			using (MemoryStream stream = new(data, false))
			{
				rootChunk = Serializer.Deserialize<OctVoxelChunk>(stream);
			}
		}
	}
}