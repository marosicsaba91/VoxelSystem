using MUtility;
using System.Linq;
using UnityEngine;

namespace VoxelSystem
{
	enum DirectionType { General, Diagonal2D, Diagonal3D }

	static class DirectionTypeHelper
	{
		static readonly Direction3D[] generaDirections =
			{ Direction3D.Right, Direction3D.Up, Direction3D.Forward, Direction3D.Left, Direction3D.Down, Direction3D.Back };
		static readonly Direction3D[] diagonal2DDirections =
			{ Direction3D.Right, Direction3D.Up, Direction3D.Forward, Direction3D.Left, Direction3D.Down, Direction3D.Back,
			Direction3D.RightUp, Direction3D.RightDown, Direction3D.LeftUp, Direction3D.LeftDown,
			Direction3D.RightForward, Direction3D.RightBack, Direction3D.LeftForward, Direction3D.LeftBack,
			Direction3D.UpForward, Direction3D.UpBack, Direction3D.DownForward, Direction3D.DownBack};
		static readonly Direction3D[] diagonal3DDirections = DirectionUtility.direction3DValues;

		static readonly Vector3Int[] generaDirectionVectors;
		static readonly Vector3Int[] diagonal2DDirectionVectors;
		static readonly Vector3Int[] diagonal3DDirectionVectors;
		

		static DirectionTypeHelper()
		{
			generaDirectionVectors = generaDirections.Select(direction => direction.ToVectorInt()).ToArray();
			diagonal2DDirectionVectors = diagonal2DDirections.Select(direction => direction.ToVectorInt()).ToArray();
			diagonal3DDirectionVectors = diagonal3DDirections.Select(direction => direction.ToVectorInt()).ToArray();
		}

		public static Direction3D[] GetDirections(this DirectionType allowedDirections) => allowedDirections switch
		{
			DirectionType.General => generaDirections,
			DirectionType.Diagonal2D => diagonal2DDirections,
			DirectionType.Diagonal3D => diagonal3DDirections,
			_ => throw new System.ArgumentException()
		};

		public static Vector3Int[] GetDirectionVectors(this DirectionType allowedDirections) => allowedDirections switch
		{
			DirectionType.General => generaDirectionVectors,
			DirectionType.Diagonal2D => diagonal2DDirectionVectors,
			DirectionType.Diagonal3D => diagonal3DDirectionVectors,
			_ => throw new System.ArgumentException()
		};
	}
}