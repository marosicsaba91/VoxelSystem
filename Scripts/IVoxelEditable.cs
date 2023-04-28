using UnityEngine;

namespace VoxelSystem
{
	public interface IVoxelEditable
	{

		VoxelMap Map { get; }
		Transform transform { get; }
		ScriptableObject SharedMap { get; }
		Object Object { get; }
		IVoxelBuilder Builder { get; }
		bool LockPosition { get; set; }
		bool LockRotation { get; set; }
		bool LockScale { get; set; }

		void ApplyRotation();
		void ApplyScale();
		void CopyMapFrom(VoxelMap originalMap);
		bool HasConnectedMap();
		void RegenerateMesh();
	}
}
