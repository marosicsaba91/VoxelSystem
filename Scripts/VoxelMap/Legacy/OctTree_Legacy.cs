using System;
using UnityEngine;
using MUtility;

namespace VoxelSystem
{
	// This version of OctTree is not used anymore, because it's inefficient 

	[Serializable]
	public class OctTree_Legacy : ISerializationCallbackReceiver
	{
		const int defaultCanvasSize = 8;

		public Vector3Int canvasSize;
		public int levels;

		[SerializeField] byte[] data;

		[NonSerialized] public OctTreeNode_Legacy rootChunk;

		// Actions --------------------------------------------

		public event Action MapChangedEvent;

		void MapChanged() =>
			MapChangedEvent?.Invoke();

		public void UndoRedoEvenInvokedOnMap() =>
			MapChangedEvent?.Invoke();

		// Voxel Map Info --------------------------------------------

		int RealSize
		{
			get
			{
				int s = 1;
				for (int i = 0; i < levels; i++)
				{
					s *= 2;
				}
				return s;
			}
		}

		public Vector3Int Size => canvasSize;

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

		public object ChunkCount => rootChunk.ChunkCount;

		public bool IsValidCoord(int x, int y, int z)
		{
			if (rootChunk == null)return false;

			return
				x >= 0 && x < canvasSize.x &&
				y >= 0 && y < canvasSize.y &&
				z >= 0 && z < canvasSize.z;
		}

		public bool IsValidCoord(Vector3Int coordinate) => IsValidCoord(coordinate.x, coordinate.y, coordinate.z);

		// Constructor -----------------------------------------------------------------------------

		public OctTree_Legacy()
		{
			Setup(new Vector3Int(defaultCanvasSize, defaultCanvasSize, defaultCanvasSize));
		}

		public OctTree_Legacy(int x, int y, int z)
		{
			Setup(new Vector3Int(x, y, z));
		}

		public OctTree_Legacy(Vector3Int canvasSize, int value = -1)
		{
			Setup(canvasSize, value);
		}

		void Setup(Vector3Int canvasSize, int value = -1)
		{

			bool isSizeInvalid = canvasSize.x <= 0 || canvasSize.y <= 0 || canvasSize.z <= 0;
			if (isSizeInvalid)
			{
				this.canvasSize = Vector3Int.zero;
				rootChunk = new OctTreeNode_Legacy();
				return;
			}

			int longestCanvasSize = Mathf.Max(canvasSize.x, canvasSize.y, canvasSize.z);
			levels = Mathf.CeilToInt(Mathf.Log(longestCanvasSize, 2));
			this.canvasSize = canvasSize;
			rootChunk = new OctTreeNode_Legacy(value);
		}

		SubVoxel GetSubChunk(ref int x, ref int y, ref int z, int size)
		{
			if (x > size / 2)
			{
				x = size / 2;
				if (y > size / 2)
				{
					y = size / 2;
					if (z > size / 2)
					{
						z = size / 2;
						return SubVoxel.RightUpForward;
					}
					return SubVoxel.RightUpBackward;
				}

				if (z > size / 2)
				{
					z = size / 2;
					return SubVoxel.RightDownForward;
				}
				return SubVoxel.RightDownBackward;
			}

			if (y > size / 2)
			{
				y = size / 2;
				if (z > size / 2)
				{
					z = size / 2;
					return SubVoxel.LeftUpForward;
				}
				return SubVoxel.LeftUpBackward;
			}

			if (z > size / 2)
			{
				z = size / 2;
				return SubVoxel.LeftDownForward;
			}
			return SubVoxel.LeftDownBackward;
		}

		// GET Voxels --------------------------------------------------------        

		public int Get(int x, int y, int z)
		{
			return rootChunk.GetLeaf(x, y, z, RealSize);
		}

		public bool IsFilledSafe(Vector3Int index) => index.x >= 0 && index.x < canvasSize.x &&
				   index.y >= 0 && index.y < canvasSize.y &&
				   index.z >= 0 && index.z < canvasSize.z &&
				   Get(index.x, index.y, index.z) != OctTreeNode_Legacy.defaultValue;

		// SET Voxels --------------------------------------------------------

		public enum VoxelAreaAction { Fill, Clear, Repaint }

		public bool Set(int x, int y, int z, VoxelAreaAction action, int materialIndex)
		{
			return rootChunk.SetLeaf(x, y, z, materialIndex, RealSize);
		}

		public bool Set(Vector3Int coordinate, VoxelAreaAction action, int materialIndex) => Set(coordinate.x, coordinate.y, coordinate.z, action, materialIndex);

		public bool Set(Vector3Int coordinate, int materialIndex) => Set(coordinate.x, coordinate.y, coordinate.z, VoxelAreaAction.Fill, materialIndex);

		public bool Set(int x, int y, int z, int materialIndex) => Set(x, y, z, VoxelAreaAction.Fill, materialIndex);

		public bool SetValueOf(Vector3Int coordinate, int materialIndex) => Set(coordinate.x, coordinate.y, coordinate.z, VoxelAreaAction.Repaint, materialIndex);

		public bool SetValueOf(int x, int y, int z, int materialIndex) => Set(x, y, z, VoxelAreaAction.Repaint, materialIndex);

		public bool ClearVoxel(Vector3Int coordinate) => ClearVoxel(coordinate.x, coordinate.y, coordinate.z);

		public bool ClearVoxel(int x, int y, int z) => Set(x, y, z, VoxelAreaAction.Clear, materialIndex: 0);

		public void ClearWhole(Vector3Int canvasSize)
		{
			this.canvasSize = canvasSize;
			rootChunk.Fill(OctTreeNode_Legacy.defaultValue);
			MapChanged();
		}

		public void FillWhole(int materialIndex)
		{
			rootChunk.Fill(materialIndex);
			MapChanged();
		}



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
			//rootChunk = (OctTreeNode)formatter.Deserialize(stream);
		}
	}
}