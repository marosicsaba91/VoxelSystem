using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;

namespace VoxelSystem
{
	public static class BlockVoxelUtility
	{
		static readonly List<OctoBlockType> _allBlockType;
		static readonly List<Axis3D> _allAxis;
		public static IReadOnlyList<OctoBlockType> AllBlockType => _allBlockType;
		public static IReadOnlyList<Axis3D> AllAxis => _allAxis;

		static BlockVoxelUtility()
		{
			_allBlockType = Enum.GetValues(typeof(OctoBlockType)).Cast<OctoBlockType>().ToList();
			_allAxis = Enum.GetValues(typeof(Axis3D)).Cast<Axis3D>().ToList();
		}



		public static bool HaveAxis(this OctoBlockType blockType)
		{
			if (blockType == OctoBlockType.CornerPositive)
				return false;
			if (blockType == OctoBlockType.CornerNegative)
				return false;
			if (blockType == OctoBlockType.CrossCorner)
				return false;

			return true;
		}

	}
}