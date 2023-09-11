using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[Serializable]
	public class CubeVoxelPaletteItem : IVoxelPaletteItem
	{
		// public Vector2Int textureIndex;
		public string name;
		public Color color;

		public string Name => name;
		public Color Color => color;


		Dictionary<(GeneralDirection3D, int), CustomMesh> meshes;

		static readonly int[] positiveWinding = { 0, 1, 2, 0, 2, 3 };
		static readonly int[] negativeWinding = { 0, 2, 1, 0, 3, 2 };

		public void FreshMeshes()
		{ 
			if (meshes != null) 
				return;

			meshes = new Dictionary<(GeneralDirection3D, int), CustomMesh>();

			GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;
			for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
			{
				GeneralDirection3D direction = directions[dirIndex];
				Vector3Int normal = direction.ToVectorInt();

				GeneralDirection3D d1 = direction.Previous();
				GeneralDirection3D d2 = direction.Next();
				
				if (!d1.IsPositive())
					d1 = d1.Opposite();
				if (!d2.IsPositive())
					d2 = d2.Opposite();

				Vector3 n = (Vector3)normal;
				Vector3 p1 = d1.ToVector() * 0.5f;
				Vector3 p2 = d2.ToVector() * 0.5f;
				Vector3 nh = n * 0.5f;

				CustomMesh customMesh = new()
				{
					vertices = new[] { nh - p1 - p2, nh - p1 + p2, nh + p1 + p2, nh + p1 - p2 },
					normals = new[] { n, n, n, n },
					uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) },
					triangles = direction.IsPositive() ? positiveWinding : negativeWinding
				};
				meshes.Add((direction, 0), customMesh);
			}
		}

		public CustomMesh GetMesh(GeneralDirection3D dir) => meshes[(dir, 0)];

		// TODO: UV SUPPORT
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
				Vector2 tl = new(x / 16f, y / 16f);
				Vector2 tr = new((x + 1) / 16f, y / 16f);
				Vector2 bl = new(x / 16f, (y + 1) / 16f);
				Vector2 br = new((x + 1) / 16f, (y + 1) / 16f);

				// Add the texture quad to the dictionary
				result[i] = new TextureQuad(tl, tr, bl, br);
			}

			// Return the Quad Dictionary for use when creating faces
			return result;
		}
	}
}