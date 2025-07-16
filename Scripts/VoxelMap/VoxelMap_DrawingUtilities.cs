using MUtility;
using Shapes;
using System.Collections.Generic; 
using UnityEngine; 

namespace VoxelSystem
{
	public static class VoxelMap_DrawingUtilities
	{
		struct Line
		{
			public Vector3Int index;
			public Axis3D axis;

			public Line(Vector3Int point, Axis3D axis)
			{
				this.index = point;
				this.axis = axis;
			}
		}
		struct Limits
		{
			public int min;
			public int max;

			public Limits(int min, int max)
			{
				this.min = min;
				this.max = max;
			}
		}

		public static WireShape GetSidesDrawable(HashSet<Vector3Int> indexes, float offset = 0)
		{
			HashSet<(Vector3Int, Axis3D)> sides = GetSides(indexes);

			WireShape drawable = SidesToDrawable(sides, offset);

			return drawable;
		}

		public static WireShape GetContourDrawable(HashSet<Vector3Int> indices)
		{
			HashSet<(Vector3Int, Axis3D)> sides = GetSides(indices);
			HashSet<(Vector3Int, Vector3Int)> lines = GetLines(sides);  
			WireShape edgesDrawable = EdgesToDrawable(lines);
			return edgesDrawable;
		}

		public static WireShape GetIndicesDrawable(HashSet<Vector3Int> indices)
		{
			Vector3 half = Vector3.one * 0.5f;
			WireShape drawable = new(new List<Vector3[]>());
			foreach (Vector3Int index in indices)
			{ 
				Cuboid c = new(Vector3.one * 0.2f);
				WireShape d = c.ToDrawable();
				d.Translate(index + half);
				drawable.Merge(d); 
			}
			return drawable;
		}

		static HashSet<(Vector3Int, Vector3Int)> GetLines(HashSet<(Vector3Int, Axis3D)> sides)
		{
			HashSet<(Vector3Int, Vector3Int)> edges = new();
			foreach ((Vector3Int, Axis3D) side in sides)
			{
				Vector3Int position = side.Item1;

				Axis3D axis = side.Item2;
				Vector3Int axisDir = axis.ToPositiveDirection().ToVectorInt();

				Axis3D nextAxis = axis.Next();
				Vector3Int nextDir = nextAxis.ToPositiveDirection().ToVectorInt();

				Axis3D previousAxis = axis.Previous();
				Vector3Int previousDir = previousAxis.ToPositiveDirection().ToVectorInt();

				if (!sides.Contains((position + nextDir, axis)))
				{
					Vector3Int p1 = position + axisDir + nextDir; 
					Vector3Int p2 = p1;
					p2.SetAxis(previousAxis, p1.GetAxis(previousAxis) + 1);
					edges.Add((p1, p2));
				}
				if (!sides.Contains((position + previousDir, axis)))
				{
					Vector3Int p1 = position + axisDir + previousDir;
					Vector3Int p2 = p1;
					p2.SetAxis(nextAxis, p1.GetAxis(nextAxis) + 1);
					edges.Add((p1, p2));
				}

				if (!sides.Contains((position - nextDir, axis)))
				{
					Vector3Int p1 = position + axisDir;
					Vector3Int p2 = p1;
					p2.SetAxis(previousAxis, p1.GetAxis(previousAxis) + 1);
					edges.Add((p1, p2));
				}

				if (!sides.Contains((position - previousDir, axis)))
				{
					Vector3Int p1 = position + axisDir;
					Vector3Int p2 = p1; 
					p2.SetAxis(nextAxis, p1.GetAxis(nextAxis) +1);
					edges.Add((p1, p2));
				}
			}

			return edges;
			 
		}

		static WireShape EdgesToDrawable(HashSet<(Vector3Int, Vector3Int)> lines)
		{
			WireShape drawable = new(new List<Vector3[]>());
			foreach ((Vector3Int a, Vector3Int b) in lines)
				drawable.AddPolygon(new Vector3[] { a, b });
			return drawable;
		}

		static WireShape SidesToDrawable(HashSet<(Vector3Int, Axis3D)> sides, float offset = 0)
		{
			WireShape drawable = new(new List<Vector3[]>());
			Vector3 half = Vector3.one * 0.5f;
			foreach ((Vector3Int index, Axis3D axis) in sides)
			{
				Vector3 d = axis.ToPositiveDirection().ToVector() * 0.5f;
				Vector3 center = (Vector3)index + half + d;
				Vector3 o1 = axis.Next().ToVector() * ((1 + offset) * 0.5f);
				Vector3 o2 = axis.Previous().ToVector() * ((1 + offset) * 0.5f);
				Vector3[] polygon = new Vector3[5];
				polygon[0] = center + o1 + o2;
				polygon[1] = center + o1 - o2;
				polygon[2] = center - o1 - o2;
				polygon[3] = center - o1 + o2;
				polygon[4] = center + o1 + o2;
				drawable.AddPolygon(polygon);
			}

			return drawable;
		}

		static HashSet<(Vector3Int, Axis3D)> GetSides(HashSet<Vector3Int> indexes)
		{
			HashSet<(Vector3Int, Axis3D)> sides = new(); // (index, side)

			foreach (Vector3Int item in indexes)
			{
				for (int i = 0; i < DirectionUtility.generalDirection3DValues.Length; i++)
				{
					GeneralDirection3D direction = DirectionUtility.generalDirection3DValues[i];
					Vector3Int directionVector = direction.ToVectorInt();
					Vector3Int neighbour = item + directionVector;

					if (!indexes.Contains(neighbour))
					{
						Vector3Int index = item;
						if (!direction.IsPositive())
							index += directionVector;
						sides.Add((index, direction.GetAxis()));
					}
				}
			}

			return sides;
		}
	}
}
