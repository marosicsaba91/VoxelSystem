// using ProtoBuf;
using System;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	//[ProtoContract]
	public abstract class OctNode<TValue, TSelf> where TSelf : OctNode<TValue, TSelf>
	{
		//[ProtoMember(1)]
		public TValue value;

		//[ProtoMember(2)]
		public TSelf[] innerChunks;

		public OctNode(TValue value)
		{
			this.value = value;
		}
		public OctNode()
		{
			value = DefaultValue;
		}

		public abstract TSelf CreateNew(TValue value);


		public abstract TValue DefaultValue { get; }

		public bool IsMixed => innerChunks != null;

		public bool IsHomogeneous => innerChunks == null;

		public int ChunkCount
		{
			get
			{
				int chunkCount = 1;
				if (innerChunks != null)
					for (int i = 0; i < 8; i++)
					{
						if (innerChunks[i] == null)
							continue;
						chunkCount += innerChunks[i].ChunkCount;
					}
				return chunkCount;
			}
		}

		public TValue Value => value;


		public virtual TSelf TryGetInnerNode(int i) => innerChunks?[i];

		// ----------------------------------------------------------

		internal void Fill(TValue newValue)
		{
			innerChunks = null;
			value = newValue;
		}

		internal TValue GetLeaf(int x, int y, int z, int size)
		{
			if (innerChunks == null)
				return value;
			int index = GetSubChunkIndex(ref x, ref y, ref z, size);
			TSelf inner = innerChunks[index];
			if (inner == null)
				return value;
			return inner.GetLeaf(x, y, z, size / 2);
		}

		public void SetLeaf(int x, int y, int z, TValue newValue, int chunkSize, out bool changed, out bool homogenous)
		{
			if (innerChunks == null)  // It was homogeneous before
			{
				if (Equals(value, newValue))
				{
					changed = false;
					homogenous = true;
					return;
				}

				if (chunkSize == 1) // Leaf Node
				{
					value = newValue;
					changed = true;
					homogenous = true;
					return;
				}

				//Inner Nodes needs to be created

				innerChunks = new TSelf[8];
				int index_ = GetSubChunkIndex(ref x, ref y, ref z, chunkSize);
				TSelf newNode = CreateNew(value);
				innerChunks[index_] = newNode;
				newNode.SetLeaf(x, y, z, newValue, chunkSize / 2, out _, out _);
				changed = true;
				homogenous = false;
				return;
			}

			// It was mixed before

			int index = GetSubChunkIndex(ref x, ref y, ref z, chunkSize);
			TSelf innerChunk = innerChunks[index];

			bool innerHomogenous;
			if (innerChunk == null) // Inner chunk was homogeneous
			{
				if (Equals(value, newValue)) // Inner Chunk had the same value
				{
					changed = false;
					homogenous = false;
					return;
				}

				// Inner Chunk had a different value
				innerChunk = CreateNew(value);
				innerChunks[index] = innerChunk;
				innerChunk.SetLeaf(x, y, z, newValue, chunkSize / 2, out changed, out innerHomogenous);
			}
			else  // Inner chunk was mixed before
			{
				innerChunk.SetLeaf(x, y, z, newValue, chunkSize / 2, out changed, out innerHomogenous);
			}

			if (innerHomogenous && changed) // Inner chunk became homogeneous
			{
				if (Equals(innerChunk.value, value))
				{
					innerChunks[index] = null;

					bool allSame = true;
					for (int i = 0; i < 8; i++)
					{
						if (innerChunks[i] == null)
							continue;

						allSame = false;
						break;
					}
					if (allSame)  // Become homogeneous whit the same value as the parent
					{
						innerChunks = null;
						changed = true;
						homogenous = true;
						return;
					}
				}
				else
				{
					TValue testValue = innerChunk.value;
					bool allSame = true;
					for (int i = 0; i < 8; i++)
					{
						if (innerChunks[i] != null && Equals(innerChunks[i].value, testValue))
							continue;

						allSame = false;
						break;
					}


					if (allSame) // Become homogeneous whit different value
					{
						innerChunks = null;
						value = testValue;
						changed = true;
						homogenous = true;
						return;
					}
				}
			}

			homogenous = false;
		}

		internal void GetString(int level, string id)
		{
			string state;
			if (Equals(value, DefaultValue))
				state = "Empty";
			else state = innerChunks == null ? $"Full: {value}" : "Mixed";

			Debug.Log($"{id} --- {state} ---------------------------------------");
			if (level == 0)
				return;

			for (int i = 0; i < 8; i++)
			{
				string iid = id + $" / ({i / 4},{(i % 4) / 2},{i % 2})";
				TSelf inner = innerChunks[i];
				if (inner != null)
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

		public abstract bool Equals(TValue a, TValue b);
	}
}