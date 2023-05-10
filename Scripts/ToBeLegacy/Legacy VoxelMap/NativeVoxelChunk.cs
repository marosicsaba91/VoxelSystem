
using System.Collections.Generic;
using System.Text;

namespace VoxelSystem {
	public struct NativeVoxelChunk
	{
		public const int defaultValue = -1;
		const int mixedValue = int.MaxValue;

		public int value;

		public int leftDownBack;
		public int leftDownFront;
		public int leftUpBack;
		public int leftUpFront;
		public int rightDownBack;
		public int rightDownFront;
		public int rightUpBack;
		public int rightUpFront;

		public bool IsHomogenous => value != mixedValue;
		public bool IsMixed => value == mixedValue;
		public int Value => value;


		public NativeVoxelChunk(int value) 
		{
			this.value = value;
			leftDownBack = 0;
			leftDownFront = 0;
			leftUpBack = 0;
			leftUpFront = 0;
			rightDownBack = 0;
			rightDownFront = 0;
			rightUpBack = 0;
			rightUpFront = 0;
		}


		public int this[int key]
		{
			get => key switch
				{
					0 => leftDownBack,
					1 => leftDownFront,
					2 => leftUpBack,
					3 => leftUpFront,
					4 => rightDownBack,
					5 => rightDownFront,
					6 => rightUpBack,
					7 => rightUpFront,
					_ => throw new System.ArgumentOutOfRangeException(nameof(key), key, null)
				};

			set
			{ 
				if (key ==0) leftDownBack = value;
				else if (key ==1) leftDownFront = value;
				else if (key ==2) leftUpBack = value;
				else if (key ==3) leftUpFront = value;
				else if (key ==4) rightDownBack = value;
				else if (key ==5) rightDownFront = value;
				else if (key ==6) rightUpBack = value;
				else if (key ==7) rightUpFront = value;
				else throw new System.ArgumentOutOfRangeException(nameof(key), key, null);
			}
		}


		public bool TryGetInnerChunk(int i, out int innerChunkIndex)
		{
			if (IsHomogenous)
			{
				innerChunkIndex = default;
				return false;
			}
			innerChunkIndex = this[i];
			return true;
		}


		internal void Fill(int newValue) => value = newValue;

		internal int GetLeaf(int x, int y, int z, int size, List<NativeVoxelChunk> all)
		{
			if (IsHomogenous) // IsHomogenous
				return value;
			int subChunkIndex = GetSubChunkIndex(ref x, ref y, ref z, size);
			int globalIndex = this[subChunkIndex];
			NativeVoxelChunk inner = all[globalIndex];
			return inner.GetLeaf(x, y, z, size / 2, all);
		}

		public bool SetLeaf(int x, int y, int z, int newValue, int chunkSize, int currentIndex, List<NativeVoxelChunk> all)
		{
			if (IsHomogenous)  // IsHomogenous
			{
				if (value == newValue)
					return false;

				if (chunkSize == 1) // Leaf Node
				{
					value = newValue;
					all[currentIndex] = this;
					return true;
				}
				else // Inner Node
				{  
					for (int i = 0; i < 8; i++)
					{
						int innerIndex = all.Count;
						all.Add(new(value));
						this[i] = innerIndex;
					}
					value = mixedValue; 
					all[currentIndex] = this;
				}
			}

			int subChunkIndex = GetSubChunkIndex(ref x, ref y, ref z, chunkSize);
			int globalIndex = this[subChunkIndex]; 
			bool changed = all[globalIndex].SetLeaf(x, y, z, newValue, chunkSize / 2, globalIndex, all);

			if (changed && IsHomogenousInside(all))
				value = newValue;

			if(changed)
				all[currentIndex] = this;
			return changed;
		}

		bool IsHomogenousInside(List<NativeVoxelChunk> all)
		{
			int globalIndex = this[0];
			NativeVoxelChunk inner = all[globalIndex];
			if (inner.IsMixed)
				return false;

			int v0 = inner.value;

			for (int i = 1; i < 8; i++)
			{
				globalIndex = this[i];
				inner = all[globalIndex];
				if (inner.IsMixed)
					return false;

				if (inner.value != v0)
					return false;
			}

			return true;
		}

		static readonly StringBuilder sb = new();

		public string GetString(int level, string id, List<NativeVoxelChunk> all) => GetString(level, level, id, all);

		string GetString(int level, int maxLevel, string id, List<NativeVoxelChunk> all)
		{
			sb.Clear();

			for (int i = 0; i < maxLevel - level; i++)
				sb.Append("   ");

			sb.AppendLine(id);
			sb.Append(": ");

			if (IsHomogenous)
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
				int global = this[i];
				sb.AppendLine(all[global].GetString(level - 1, maxLevel, iid, all));
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

		// --------- STATIC ---------

		static List<NativeVoxelChunk> cleanUpList;
		static void CleanUp(List<NativeVoxelChunk> chunks)
		{
			// NOT RIGHT
			cleanUpList.Clear();
			NativeVoxelChunk root = chunks[0];
			cleanUpList.Add(root);
			CleanUp(root);

			void CleanUp(NativeVoxelChunk chunk)
			{
				if (chunk.IsHomogenous) return;
				for (int i = 0; i < 8; i++)
				{
					int subChunkIndex = chunk[i];
					NativeVoxelChunk innerChunk = chunks[subChunkIndex];
					int newIndex = cleanUpList.Count;
					cleanUpList.Add(innerChunk);
					chunk[i] = newIndex;
					CleanUp(innerChunk);
				}
			}
			chunks.Clear();
			chunks.AddRange(cleanUpList);
		}
	}
}