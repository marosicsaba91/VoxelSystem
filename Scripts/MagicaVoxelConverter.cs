using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	public static class MagicaVoxelConverter
	{

		public static VoxelMap Load(string filePath)
		{
			FileStream fs = File.Open(filePath, FileMode.Open);

			byte[] buffer = new byte[4];

			// Read Header Magic Number
			fs.Read(buffer, offset: 0, count: 4);
			if (StringFromBuffer(buffer) != "VOX ")
				return null;

			// Read Version Number, Discard
			fs.Read(buffer, offset: 0, count: 4);

			// Read MAIN Chunk
			MVoxChunk main = ReadMVoxChunk(fs);
			return Convert(main);
		}

		static VoxelMap Convert(MVoxChunk main)
		{
			// Get the SIZE chunk
			MVoxChunk size = main.Children.SingleOrDefault(c => c.ID.Contains("SIZE"));

			// Get VOXEL chunk
			MVoxChunk voxels = main.Children.SingleOrDefault(c => c.ID.Contains("XYZI"));

			if (size != null && voxels != null)
			{
				UInt32 x, y, z;
				x = ConvertToUInt32(size.Contents.SubArray(index: 0, length: 4), offset: 0);
				y = ConvertToUInt32(size.Contents.SubArray(index: 4, length: 4), offset: 0);
				z = ConvertToUInt32(size.Contents.SubArray(index: 8, length: 4), offset: 0);

				if (x > 0 && y > 0 && z > 0)
				{
					VoxelMap model = new VoxelMap((int)x, (int)y, (int)z);

					// Read Number of voxels
					UInt32 numVoxels = ConvertToUInt32(voxels.Contents.SubArray(index: 0, length: 4), offset: 0);
					int offset = 4;

					for (int i = 0; i < numVoxels; i++)
					{
						byte vX, vY, vZ, vI;
						vX = voxels.Contents[(i * 4) + offset];
						vY = voxels.Contents[(i * 4) + offset + 1];
						vZ = voxels.Contents[(i * 4) + offset + 2];
						vI = voxels.Contents[(i * 4) + offset + 3];

						Vector3Int index = new Vector3Int(vX, vY, vZ);
						if (x > index.x && y > index.y && z > index.z)
						{
							model.Set(index, vI);
						}
					}
					return model;
				}
			}

			Console.WriteLine("SIZE or XYZI was null");

			return null;
		}

		static MVoxChunk Convert(VoxelMap map)
		{
			// TODO
			return null;
		}


		static string StringFromBuffer(byte[] buffer)
		{
			if (buffer == null)
				return string.Empty;

			string value = string.Empty;

			for (int i = 0; i < buffer.Length; i++)
			{
				value += (char)buffer[i];
			}

			return value;
		}

		static MVoxChunk ReadMVoxChunk(Stream stream)
		{
			MVoxChunk chunk = new MVoxChunk()
			{
				Size = 0,
				ChildrenSize = 0,
				ID = string.Empty,
				Contents = null
			};

			byte[] buffer = new byte[4];

			// Read ID
			if (stream.Read(buffer, offset: 0, count: 4) > 0)
				chunk.ID = StringFromBuffer(buffer);

			// Read Size
			stream.Read(buffer, offset: 0, count: 4);
			chunk.Size = ConvertToUInt32(buffer, offset: 0);

			// Read Size of Children
			if (stream.Read(buffer, offset: 0, count: 4) > 0)
				chunk.ChildrenSize = ConvertToUInt32(buffer, offset: 0);

			// Read Data
			if (chunk.Size > 0)
			{
				chunk.Contents = new byte[chunk.Size];
				byte[] tmp = new byte[1];
				for (UInt32 i = 0; i < chunk.Size; i++)
				{
					if (stream.Read(tmp, offset: 0, count: 1) > 0)
						chunk.Contents[i] = tmp[0];
					else
						chunk.Contents[i] = 0x00;
				}
			}

			// Read Children Chunks
			if (chunk.ChildrenSize > 0)
			{
				chunk.Children = new List<MVoxChunk>();

				// Recursively Read the Children chunks.
				byte[] childBuffer = new byte[chunk.ChildrenSize];
				if (stream.Read(childBuffer, offset: 0, (int)chunk.ChildrenSize) > 0)
				{
					MemoryStream mStream = new MemoryStream(childBuffer);
					while (mStream.Position < mStream.Length)
					{
						chunk.Children.Add(ReadMVoxChunk(mStream));
					}
				}
			}

			return chunk;
		}

		static UInt32 ConvertToUInt32(byte[] buffer, int offset, bool swap = false)
		{
			if (buffer != null)
			{
				if (!swap)
				{
					return BitConverter.ToUInt32(buffer, offset);
				}
				else
				{
					// XOR Swapping for endianness
					for (int i = 0; i < buffer.Length; i += 2)
					{
						buffer[i] ^= buffer[i + 1];
						buffer[i + 1] ^= buffer[i];
						buffer[i] ^= buffer[i + 1];
					}

					return BitConverter.ToUInt32(buffer, offset);
				}
			}
			else
				return (UInt32)0;
		}

		private class MVoxChunk
		{
			public string ID { get; set; }
			public UInt32 Size { get; set; }
			public UInt32 ChildrenSize { get; set; }
			public byte[] Contents { get; set; }
			public List<MVoxChunk> Children { get; set; }
		}
	}
}
