using MUtility;
using UnityEngine;

namespace VoxelSystem
{
	public class VoxelToolHandler_ColorPicker : VoxelToolHandler
	{
		protected override bool DoRaycastVoxelCursor(IVoxelEditor voxelEditor, out bool raycastOutside)
		{
			raycastOutside = false;
			return true;
		}
	}
}
