using MUtility;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "VoxelMap", menuName = "VoxelSystem/VoxelMap", order = 1)]
	public class VoxelMapScriptableObject : ScriptableObject
	{
		public ArrayVoxelMap map;
		[HideInInspector] public OctVoxelMap octMap;
		[SerializeField] DisplayMember copyToOctMap = new(nameof(CopyToOctMap));

		public int size = 0;
		public int value = -1;
		public Vector3Int index = Vector3Int.zero;
		[SerializeField] DisplayMember create = new(nameof(Create));
		[SerializeField] DisplayMember set = new(nameof(Set));

		[SerializeField] DisplayMember deserialize = new(nameof(Deserialize));
		[SerializeField] DisplayMember serialize = new(nameof(Serialize));

		void Deserialize() => octMap.DeserializeFromByteArray();
		void Serialize() => octMap.SerializeToByeArray();


		void CopyToOctMap()
		{				
			Vector3Int size = map.FullSize;
			octMap = new OctVoxelMap(size);


			for (int x = 0; x < size.x; x++)
				for (int y = 0; y < size.y; y++)
					for (int z = 0; z < size.z; z++)
					{
						int value = map.GetVoxel(x, y, z);
						octMap.SetVoxel(x, y, z, value);
					}

			int chunkCount = octMap.RootChunk.ChunkCount;
			int voxelCount = octMap.CanvasSize.x * octMap.CanvasSize.y * octMap.CanvasSize.z;
			float chunkPerVoxel = (float)chunkCount / voxelCount;
			float reductionRate = chunkPerVoxel * 100;

			Debug.Log($"1: ChunkCount:  {chunkCount}     VoxelCount: {voxelCount}     Reduction rate: {reductionRate}%");

			// Make ScriptableObject dirty
			EditorUtility.SetDirty(this);
		}


		void Create()
		{
			octMap = new OctVoxelMap(Vector3Int.one * size, value);
			EditorUtility.SetDirty(this);
		}
		void Set() => octMap.SetVoxel(index, value);
	}
}