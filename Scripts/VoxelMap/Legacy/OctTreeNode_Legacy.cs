using System;
using UnityEngine;

namespace VoxelSystem
{
	// This version of OctTreeNode is not used anymore, because it's inefficient 
	[Serializable]
	public class OctTreeNode_Legacy
	{
		public OctTreeNode_Legacy[] innerChunks;
		public int value;

		public OctTreeNode_Legacy(int value = defaultValue)
		{
			this.value = value;
		}

		public const int defaultValue = -1;

		public bool IsMixed => innerChunks != null;

		public bool IsHomogenous => innerChunks == null;

		public int ChunkCount
		{
			get
			{
				int chunkCount = 1;
				if (innerChunks != null)
					for (int i = 0; i < 8; i++)
						chunkCount += innerChunks[i].ChunkCount;
				return chunkCount;
			}
		}

		public int Value => value;
		public OctTreeNode_Legacy GetInnerNode(int i) => innerChunks[i];

		// ----------------------------------------------------------

		internal void Fill(int newValue)
		{
			innerChunks = null;
			value = newValue;
		}

		internal int GetLeaf(int x, int y, int z, int size)
		{
			if (innerChunks == null)
				return value;
			int index = GetSubChunkIndex(ref x, ref y, ref z, size);
			OctTreeNode_Legacy inner = innerChunks[index];
			return inner.GetLeaf(x, y, z, size / 2);
		}

		public bool SetLeaf(int x, int y, int z, int newValue, int chunkSize)
		{
			if (innerChunks == null)  // It was NOT mixed before
			{
				if (value == newValue)
					return false;

				if (chunkSize == 1) // Leaf Node
				{
					value = newValue;
					return true;
				}
				else // Inner Node
				{
					innerChunks = new OctTreeNode_Legacy[8];
					for (int i = 0; i < 8; i++)
						innerChunks[i] = new OctTreeNode_Legacy(value);
				}
			}

			int index = GetSubChunkIndex(ref x, ref y, ref z, chunkSize);
			bool changed = innerChunks[index].SetLeaf(x, y, z, newValue, chunkSize / 2);

			if (changed && IsHomogenousInside())
			{
				value = newValue;
				innerChunks = null;
			}

			return changed;
		}

		bool IsHomogenousInside()
		{
			if (innerChunks[0].innerChunks != null)
				return false;

			int v0 = innerChunks[0].value;
			for (int i = 1; i < 8; i++)
			{
				if (innerChunks[i].innerChunks != null)
					return false;

				if (innerChunks[i].value != v0)
					return false;
			}

			return true;
		}

		internal void GetString(int level, string id)
		{
			string state;
			if (value == defaultValue)
				state = "Empty";
			else if (innerChunks == null)
				state = $"Full: {value}";
			else
				state = "Mixed";

			Debug.Log($"{id} --- {state} ---------------------------------------");
			if (level == 0)
				return;

			for (int i = 0; i < 8; i++)
			{
				string iid = id + $" / ({i / 4},{(i % 4) / 2},{i % 2})";
				innerChunks[i].GetString(level - 1, iid);
			}
		}

		public static int GetSubChunkIndex(ref int x, ref int y, ref int z, int size)
		{
			if (x >= size / 2)
			{
				x -= size / 2;
				if (y >= size / 2)
				{
					y -= size / 2;
					if (z >= size / 2)
					{
						z -= size / 2;
						return (int)SubVoxel.RightUpForward;
					}
					return (int)SubVoxel.RightUpBackward;
				}

				if (z >= size / 2)
				{
					z -= size / 2;
					return (int)SubVoxel.RightDownForward;
				}
				return (int)SubVoxel.RightDownBackward;
			}

			if (y >= size / 2)
			{
				y -= size / 2;
				if (z >= size / 2)
				{
					z -= size / 2;
					return (int)SubVoxel.LeftUpForward;
				}
				return (int)SubVoxel.LeftUpBackward;
			}

			if (z >= size / 2)
			{
				z -= size / 2;
				return (int)SubVoxel.LeftDownForward;
			}
			return (int)SubVoxel.LeftDownBackward;
		}
	}
}