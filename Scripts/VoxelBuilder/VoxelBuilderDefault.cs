using System.Collections.Generic;
using UnityEngine;
using MUtility;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "DefaultVoxelBuilder", menuName = "VoxelSystem/DefaultVoxelBuilder", order = 3)]
	public class VoxelBuilderDefault : VoxelBuilder
	{
		protected override void BuildMesh(VoxelMap map, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles) => BuildTiledMesh(map, vertices, normals, uv, triangles);

		public override IEnumerable<PaletteItem> GetPaletteItems()
		{
			// TODO
			yield break;
		}

		public override int PaletteLength => 0;

		public static void BuildTiledMesh(VoxelMap map, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv,
			List<int> triangles)
		{
			int w = map.Size.x;
			int h = map.Size.y;
			int d = map.Size.z;
			GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;
			TextureQuad[] quadArray = GenerateTextureCoordinateArray(textureSize: 4);

			GeneralDirection3D dir;
			Vector3 normal;
			float normX;
			float normY;
			float normZ;
			Vector3 perp1;
			Vector3 perp2;
			Vector3Int dirVecI;
			int neighbourX;
			int neighbourY;
			int neighbourZ;
			Vector3 center;
			TextureQuad uvQuad;
			int vertexCount = 0;
			//   int counting = 0;


			for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
			{
				dir = directions[dirIndex];
				normal = dir.ToVector();
				normX = normal.x;
				normY = normal.y;
				normZ = normal.z;
				perp1 = dir.GetPerpendicularLeftHand().ToVector() / 2f;
				perp2 = dir.GetPerpendicularRightHand().ToVector() / 2f;
				dirVecI = dir.ToVectorInt();

				for (int x = 0; x < w; x++)
					for (int y = 0; y < h; y++)
						for (int z = 0; z < d; z++)
						{
							int colorIndex = map.GetFast(x, y, z, w, h).value;
							if (colorIndex < 0)
							{
								continue;
							}

							neighbourX = dirVecI.x + x;
							neighbourY = dirVecI.y + y;
							neighbourZ = dirVecI.z + z;

							bool neighBourOutOfBounds =
								neighbourX < 0 || neighbourY < 0 || neighbourZ < 0 ||
								neighbourX >= w || neighbourY >= h || neighbourZ >= d;


							if (!neighBourOutOfBounds)
								if (map.GetFast(neighbourX, neighbourY, neighbourZ, w, h).IsFilled)
									continue;

							uvQuad = quadArray[colorIndex];
							center.x = x + (normX / 2f) + 0.5f;
							center.y = y + (normY / 2f) + 0.5f;
							center.z = z + (normZ / 2f) + 0.5f;

							vertices.Add(center + perp1 + perp2);
							normals.Add(normal);
							uv.Add(uvQuad.topRight);

							vertices.Add(center + perp1 - perp2);
							normals.Add(normal);
							uv.Add(uvQuad.topLeft);

							vertices.Add(center - perp1 - perp2);
							normals.Add(normal);
							uv.Add(uvQuad.bottomLeft);

							vertices.Add(center - perp1 + perp2);
							normals.Add(normal);
							uv.Add(uvQuad.bottomRight);

							vertexCount += 4;
							triangles.Add(vertexCount - 4);
							triangles.Add(vertexCount - 3);
							triangles.Add(vertexCount - 2);
							triangles.Add(vertexCount - 2);
							triangles.Add(vertexCount - 1);
							triangles.Add(vertexCount - 4);
						}
			}

		}


		static TextureQuad[] GenerateTextureCoordinateArray(int textureSize = 16)
		{
			// Assume a 16x16 texture atlas
			TextureQuad[] result = new TextureQuad[textureSize * textureSize];

			// assumption based on, well, because I can
			for (int i = 0; i < textureSize * textureSize; i++)
			{
				// Calculate the Integer Array positions of the Index on a 16x16 grid
				int x = i / textureSize;
				int y = i % textureSize;

				// Create each Texture coordinate for a quad of 1/16x1/16 dimensions
				Vector2 tl = new Vector2(x / 16f, y / 16f);
				Vector2 tr = new Vector2((x + 1) / 16f, y / 16f);
				Vector2 bl = new Vector2(x / 16f, (y + 1) / 16f);
				Vector2 br = new Vector2((x + 1) / 16f, (y + 1) / 16f);

				// Add the texture quad to the dictionary
				result[i] = new TextureQuad(tl, tr, bl, br);
			}

			// Return the Quad Dictionary for use when creating faces
			return result;
		}
	}
}