using UnityEngine;

namespace VoxelSystem
{
	public abstract class SharedVoxelMap : ScriptableObject
	{
		internal abstract VoxelMap Map { get; }
	}

	public abstract class SharedVoxelMap<TVoxelMap> : SharedVoxelMap where TVoxelMap : VoxelMap
	{
		[SerializeField] internal TVoxelMap map;
		internal sealed override VoxelMap Map => map;

		/*
		[Space]
		[SerializeField] List<SharedVoxelMap> copyDestinations;
		[SerializeField] DisplayMember copyToDestinations = new(nameof(CopyMaps));
		[SerializeField] DisplayMember clearDestinations = new(nameof(ClearDestinations));

		[Space]
		[SerializeField] VoxelMapScriptableObject source;
		[SerializeField] DisplayMember copyFromSource = new(nameof(CopyFromSource));

		protected void ClearDestinations() => copyDestinations.Clear();

		protected void CopyMaps()
		{

			foreach (SharedVoxelMap item in copyDestinations)
			{
				if (item != null)
					CopyToDestinationMap(item.Map);
			}
		}
		protected void CopyToDestinationMap(VoxelMap destination)
		{
			destination.SetupFrom(map);
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		protected void CopyFromSource()
		{
			if (source == null)
			{
				Debug.LogError("Source is null");
				return;
			}

			map.SetupFrom(source.map);

			// Make ScriptableObject dirty
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
		*/
	}
}