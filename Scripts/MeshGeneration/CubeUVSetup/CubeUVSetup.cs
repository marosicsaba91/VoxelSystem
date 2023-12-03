using EasyInspector;
using MUtility;
using System;
using UnityEngine;

namespace VoxelSystem
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
	}

}