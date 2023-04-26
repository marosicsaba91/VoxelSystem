using ProtoBuf;
using System.Text;
using UnityEngine;

namespace VoxelSystem
{ 
	// This version of OctTreeNode is not used anymore, because it's inefficient  

	[ProtoContract]
	public struct OctVoxelChunk
	{
		[ProtoMember(1)] 
		public int value;
		[ProtoMember(2)]
		public OctVoxelChunk[] innerChunks;

		public const int defaultValue = -1;

		public bool IsMixed => innerChunks != null;

		public bool IsHomogenous => innerChunks == null;

		public bool IsDefault => value == defaultValue;

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

		public bool TryGetInnerChunk(int i, out OctVoxelChunk chunk)
		{
			if (innerChunks == null) 
			{
				chunk = default;
				return false;
			}
			chunk = innerChunks[i];
			return true;
		}

		// ----------------------------------------------------------

		internal void Fill(int newValue)
		{
			innerChunks = null;
			value = newValue;
		}

		internal int GetLeaf(int x, int y, int z, int size)
		{
			if (innerChunks == null) // IsHomogenous
				return value;
			int index = GetSubChunkIndex(ref x, ref y, ref z, size);
			OctVoxelChunk inner = innerChunks[index];
			return inner.GetLeaf(x, y, z, size / 2);
		}


		public bool SetLeaf(int x, int y, int z, int newValue, int chunkSize)
		{
			if (innerChunks == null)  // IsHomogenous
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
					innerChunks = new OctVoxelChunk[8];
					for (int i = 0; i < 8; i++)
					{
						innerChunks[i] = new OctVoxelChunk();
						innerChunks[i].Fill(value);
					}
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

		static readonly StringBuilder sb = new();

		public string GetString(int level, string id = "Root") => GetString(level, level, id);

		string GetString(int level, int maxLevel, string id)
		{
			sb.Clear();

			for (int i = 0; i < maxLevel - level; i++)
				sb.Append("   ");

			sb.AppendLine(id);
			sb.Append(": ");

			if (innerChunks == null)
			{
				sb.AppendLine(value == defaultValue ? "Empty" : $"Full: {value}");
				return sb.ToString();
			}
			else
				sb.AppendLine("Mixed");

			if (level == 0)
				return sb.ToString();

			for (int i = 0; i < 8; i++)
			{
				string iid = id + $" / ({i / 4},{(i % 4) / 2},{i % 2})";
				sb.AppendLine(innerChunks[i].GetString(level - 1, maxLevel, iid));
			}
			return sb.ToString();
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

	static class OctVoxelUtility
	{
		internal static bool IsFilled(this int i) => i != OctVoxelChunk.defaultValue;
	}

}