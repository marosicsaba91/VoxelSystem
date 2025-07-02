using EasyEditor;
using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem.MeshUtility
{
	[Serializable]
	public class CubeUVSetup
	{
		enum TextureType { SameSides, TopSideBottom, SixSide }

		[Range(1, 4)] public int gridSize = 1;

		[Color(DisplayColor.Red)] public Vector2Int right;
		[Color(DisplayColor.Cyan)] public Vector2Int left;
		[Color(DisplayColor.Green)] public Vector2Int top;
		[Color(DisplayColor.Magenta)] public Vector2Int bottom;
		[Color(DisplayColor.Blue)] public Vector2Int front;
		[Color(DisplayColor.Yellow)] public Vector2Int back;

		[SerializeField, HideInInspector] Rect rightRect;
		[SerializeField, HideInInspector] Rect leftRect;
		[SerializeField, HideInInspector] Rect topRect;
		[SerializeField, HideInInspector] Rect bottomRect;
		[SerializeField, HideInInspector] Rect frontRect;
		[SerializeField, HideInInspector] Rect backRect;

		static readonly List<int> positiveWinding = new() { 0, 1, 2, 0, 2, 3 };
		static readonly List<int> negativeWinding = new() { 0, 2, 1, 0, 3, 2 };

		public void OnValidate()
		{
			Vector2Int max = Vector2Int.one * (gridSize - 1);
			right = Vector2Int.Min(max, Vector2Int.Max(right, Vector2Int.zero));
			left = Vector2Int.Min(max, Vector2Int.Max(left, Vector2Int.zero));
			top = Vector2Int.Min(max, Vector2Int.Max(top, Vector2Int.zero));
			bottom = Vector2Int.Min(max, Vector2Int.Max(bottom, Vector2Int.zero));
			front = Vector2Int.Min(max, Vector2Int.Max(front, Vector2Int.zero));
			back = Vector2Int.Min(max, Vector2Int.Max(back, Vector2Int.zero));

			rightRect = GetRect(right, gridSize);
			leftRect = GetRect(left, gridSize);
			topRect = GetRect(top, gridSize);
			bottomRect = GetRect(bottom, gridSize);
			frontRect = GetRect(front, gridSize);
			backRect = GetRect(back, gridSize);
		}

		Rect GetRect(Vector2Int gridIndex, int gridSize)
		{
			float size = 1f / gridSize;
			return new Rect(gridIndex.x * size, gridIndex.y * size, size, size);
		}

		public Rect GetRect(GeneralDirection3D direction) => direction switch
		{
			GeneralDirection3D.Right => rightRect,
			GeneralDirection3D.Left => leftRect,
			GeneralDirection3D.Up => topRect,
			GeneralDirection3D.Down => bottomRect,
			GeneralDirection3D.Forward => frontRect,
			GeneralDirection3D.Back => backRect,
			_ => throw new Exception("Invalid direction")
		};

		public MeshBuilder GetCubeSide(GeneralDirection3D direction, float size = 1, bool faceOut = true, float normalOffset = 0)
		{
			MeshBuilder builder = new();
			AddCubeSide(builder, direction, size, faceOut, normalOffset);
			return builder;
		}

		public void AddCubeSide(MeshBuilder builder, GeneralDirection3D direction, float size = 1, bool faceOut = true, float normalOffset = 0)
		{
			GeneralDirection3D perpendicularDir1 = direction.GetPerpendicularNext();
			GeneralDirection3D perpendicularDir2 = direction.GetPerpendicularLeftHand(perpendicularDir1);

			if (!perpendicularDir1.IsPositive())
				perpendicularDir1 = perpendicularDir1.Opposite();
			if (!perpendicularDir2.IsPositive())
				perpendicularDir2 = perpendicularDir2.Opposite();

			Vector3 normal = (Vector3)direction.ToVectorInt();
			Vector3 p1 = perpendicularDir1.ToVector() * (size / 2f);
			Vector3 p2 = perpendicularDir2.ToVector() * (size / 2f);
			Vector3 center = normal * (size / 2 + normalOffset);

			Rect rect = GetRect(direction);
			List<int> winding = faceOut ^ direction.IsPositive() ? negativeWinding : positiveWinding;

			builder.AddVertex(center - p1 - p2, normal, rect.BottomLeft());
			builder.AddVertex(center - p1 + p2, normal, rect.TopLeft());
			builder.AddVertex(center + p1 + p2, normal, rect.TopRight());
			builder.AddVertex(center + p1 - p2, normal, rect.BottomRight());
			int startIndex = builder.VertexCount - 4;
			for (int i = 0; i < winding.Count; i++)
				builder.triangles.Add(winding[i] + startIndex);

		}
	}

}