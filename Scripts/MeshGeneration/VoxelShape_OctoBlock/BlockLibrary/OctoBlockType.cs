namespace VoxelSystem
{
	public enum OctoBlockType
	{
		SidePositive,
		EdgePositive,
		CornerPositive,

		EdgeNegative,
		CornerNegative,
		SideToPositiveEdge,
		SideToNegativeEdge,

		CrossCorner,
		EdgeToEdge,

		BreakPoint,
	}
}