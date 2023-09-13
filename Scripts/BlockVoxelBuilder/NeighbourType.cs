namespace VoxelSystem
{
	enum NeighbourType
	{
		SameFilled,
		DifferentFilled,
		EmptyOutOfMap,
		EmptyInMap
	}

	static class NeighBourTypeHelper
	{
		public static bool IsSame(this NeighbourType neighbour) => neighbour == NeighbourType.SameFilled;
		public static bool IsFilled(this NeighbourType neighbour) => neighbour is NeighbourType.DifferentFilled or NeighbourType.SameFilled;
		public static bool IsOutsideMap(this NeighbourType neighbour) => neighbour == NeighbourType.EmptyOutOfMap;
		public static bool IsEmpty(this NeighbourType neighbour) => neighbour is NeighbourType.EmptyInMap or NeighbourType.EmptyOutOfMap;
		public static bool IsInMap(this NeighbourType neighbour) => neighbour != NeighbourType.EmptyOutOfMap;
		public static bool SameOrOut(this NeighbourType neighbour) => neighbour is NeighbourType.SameFilled or NeighbourType.EmptyOutOfMap;
	}

}