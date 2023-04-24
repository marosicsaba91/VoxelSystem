using System;
using UnityEngine;
using MUtility;

namespace VoxelSystem
{
	[Serializable]
	public abstract class OctTree<TNode, TValue> where TNode : OctNode<TValue, TNode>
	{
		public int levels;
		[NonSerialized] public TNode rootChunk;

		public Vector3Int canvasSize;

		// Actions --------------------------------------------

		public event Action MapChangedEvent;

		public abstract TValue DefaultValue { get; }

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

		public object ChunkCount => rootChunk.ChunkCount;

		public bool IsValidCoord(int x, int y, int z)
		{
			if (rootChunk == null)
			{ return false; }
			return
				x >= 0 && x < canvasSize.x &&
				y >= 0 && y < canvasSize.y &&
				z >= 0 && z < canvasSize.z;
		}

		public bool IsValidCoord(Vector3Int coordinate) => IsValidCoord(coordinate.x, coordinate.y, coordinate.z);

		// Constructor --------------------------------------------------------

		public OctTree(Vector3Int canvasSize, TValue value) 
		{ 

			bool isSizeInvalid = canvasSize.x <= 0 || canvasSize.y <= 0 || canvasSize.z <= 0;
			if (isSizeInvalid)
			{
				throw new ArgumentException("Canvas size must be greater than zero");
			}

			int longestCanvasSize = Mathf.Max(canvasSize.x, canvasSize.y, canvasSize.z);
			levels = Mathf.CeilToInt(Mathf.Log(longestCanvasSize, 2));
			this.canvasSize = canvasSize;
			rootChunk = CreateNewNode(value);
		}


		// GET Voxels --------------------------------------------------------        

		public TValue Get(int x, int y, int z) => rootChunk.GetLeaf(x, y, z, RealSize);

		public bool IsFilledSafe(Vector3Int index) => index.x >= 0 && index.x < canvasSize.x &&
				   index.y >= 0 && index.y < canvasSize.y &&
				   index.z >= 0 && index.z < canvasSize.z &&
				   !Equals(Get(index.x, index.y, index.z) , DefaultValue);

		// SET Voxels --------------------------------------------------------

		public enum ActionType { All, AllExceptDefault }

		public bool Set(int x, int y, int z, ActionType action, TValue value)
		{
			if (action == ActionType.AllExceptDefault)
			{
				TValue v = rootChunk.GetLeaf(x, y, z, RealSize);
				if (Equals(v, DefaultValue))
					return false;
			}

			rootChunk.SetLeaf(x, y, z, value, RealSize, out bool changed, out _);
			return changed;
		}

		public bool Set(Vector3Int coordinate, ActionType action, TValue value) => Set(coordinate.x, coordinate.y, coordinate.z, action, value);

		public bool Set(Vector3Int coordinate, TValue value) => Set(coordinate.x, coordinate.y, coordinate.z, ActionType.All, value);

		public bool Set(int x, int y, int z, TValue value) => Set(x, y, z, ActionType.All, value);

		public bool ReColor(Vector3Int coordinate, TValue materialIndex) => Set(coordinate.x, coordinate.y, coordinate.z, ActionType.AllExceptDefault, materialIndex);

		public bool ReColor(int x, int y, int z, TValue value) => Set(x, y, z, ActionType.AllExceptDefault, value);

		public bool ClearVoxel(Vector3Int coordinate) =>
			Set(coordinate.x, coordinate.y, coordinate.z, ActionType.All, value: DefaultValue);


		public void ClearWhole(Vector3Int canvasSize)
		{
			this.canvasSize = canvasSize;
			rootChunk.Fill(DefaultValue);
			MapChanged();
		}

		public void FillWhole(TValue value)
		{
			rootChunk.Fill(value);
			MapChanged();
		}

		public virtual bool Equals(TValue a, TValue b) => a.Equals(b);
		public abstract TNode CreateNewNode(TValue value);
	}
}