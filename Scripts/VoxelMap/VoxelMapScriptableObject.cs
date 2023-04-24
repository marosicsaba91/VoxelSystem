using MUtility;
using System;
using UnityEditor;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "VoxelMap", menuName = "VoxelSystem/VoxelMap", order = 1)]
	public class VoxelMapScriptableObject : ScriptableObject
	{
		public VoxelMap map;
		[HideInInspector] public OctVoxelMap octMap;
		[SerializeField] DisplayMember copyToOctMap = new(nameof(CopyToOctMap));

		public int size = 0;
		public int value = -1;
		public Vector3Int index = Vector3Int.zero;
		[SerializeField] DisplayMember create = new(nameof(Create));
		[SerializeField] DisplayMember set = new(nameof(Set));

		[SerializeField] DisplayMember deserialize = new(nameof(Deserialize));
		[SerializeField] DisplayMember serialize = new(nameof(Serialize));

		void Deserialize() => octMap.Deserialize();
		void Serialize() => octMap.Serialize();

		void CopyToOctMap()
		{				
			octMap = new OctVoxelMap(map.Size);
			for (int x = 0; x < map.Width; x++)
				for (int y = 0; y < map.Height; y++)
					for (int z = 0; z < map.Depth; z++)
					{
						int value = map.Get(x, y, z).value;
						octMap.Set(x, y, z, value);
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
		void Set() => octMap.Set(index, value);
	}
}